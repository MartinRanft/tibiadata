using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Corpses;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Corpses.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Corpses
{
    public sealed class CorpsesDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : ICorpsesDataBaseService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<IReadOnlyList<CorpseListItemResponse?>> GetCorpseNamesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "corpses:names",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Corpse)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article =>
                                   {
                                       Dictionary<string, string>? infobox = ParseCorpseInfobox(article.InfoboxJson);
                                       return IsCorpseDetailArticle(article, infobox)
                                       ? MapCorpseListItem(article, infobox)
                                       : null;
                                   })
                                   .Where(item => item is not null)
                                   .Cast<CorpseListItemResponse?>()
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Corpses],
                cancellationToken);
        }

        public async Task<CorpseDetailsResponse?> GetCorpseDetailsByNameAsync(string corpseName, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"corpses:by-name:{EntityNameNormalizer.Normalize(corpseName)}",
                async ct =>
                {
                    string normalizedCorpseName = EntityNameNormalizer.Normalize(corpseName);

                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Corpse)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.NormalizedTitle == normalizedCorpseName)
                                                   .SingleOrDefaultAsync(ct);

                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseCorpseInfobox(article.InfoboxJson);
                    return IsCorpseDetailArticle(article, infobox)
                    ? MapCorpseDetails(article, infobox)
                    : null;
                },
                _cacheOptions,
                [CacheTags.Corpses],
                cancellationToken);
        }

        public async Task<CorpseDetailsResponse?> GetCorpseDetailsByIdAsync(int corpseId, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"corpses:by-id:{corpseId}",
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Corpse)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == corpseId)
                                                   .SingleOrDefaultAsync(ct);

                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseCorpseInfobox(article.InfoboxJson);
                    return IsCorpseDetailArticle(article, infobox)
                    ? MapCorpseDetails(article, infobox)
                    : null;
                },
                _cacheOptions,
                [CacheTags.Corpses],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetCorpseSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "corpses:sync-states",
                async ct =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.Corpse)
                                   .Where(x => !x.IsMissingFromSource)
                                   .OrderByDescending(x => x.LastUpdated)
                                   .ThenByDescending(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       x.LastSeenAt))
                                   .ToListAsync(ct);
                });
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetCorpseSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"corpses:sync-states-bydatetime:{time:O}",
                async ct =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.Corpse)
                                   .Where(x => !x.IsMissingFromSource)
                                   .Where(x => x.LastUpdated >= time)
                                   .OrderByDescending(x => x.LastUpdated)
                                   .ThenByDescending(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       x.LastSeenAt))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Corpses],
                cancellationToken);
        }

        private static Dictionary<string, string>? ParseCorpseInfobox(string? infoboxJson)
        {
            if(string.IsNullOrWhiteSpace(infoboxJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(infoboxJson, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool IsCorpseDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            if(!string.Equals(normalizedTemplate, "Infobox Corpse", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name")) ||
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "corpseof"));
        }

        private static CorpseListItemResponse MapCorpseListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new CorpseListItemResponse(
                article.Id,
                GetCorpseName(article, infobox),
                BuildCorpseSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static CorpseDetailsResponse MapCorpseDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new CorpseDetailsResponse(
                article.Id,
                GetCorpseName(article, infobox),
                BuildCorpseSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static CorpseStructuredDataResponse? CreateStructuredData(
            WikiArticle article,
            IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) &&
               (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new CorpseStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapCorpseInfobox(infobox));
        }

        private static CorpseInfoboxResponse? MapCorpseInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new CorpseInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "actualname"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "article"),
                GetInfoboxValue(infobox, "corpseof"),
                GetInfoboxValue(infobox, "liquid"),
                GetInfoboxValue(infobox, "skinable"),
                GetInfoboxValue(infobox, "product"),
                GetInfoboxValue(infobox, "sellto"),
                GetInfoboxValue(infobox, "notes"),
                GetInfoboxValue(infobox, "flavortext"),
                GetInfoboxValue(infobox, "1decaytime"),
                GetInfoboxValue(infobox, "2decaytime"),
                GetInfoboxValue(infobox, "3decaytime"),
                GetInfoboxValue(infobox, "1volume"),
                GetInfoboxValue(infobox, "2volume"),
                GetInfoboxValue(infobox, "3volume"),
                GetInfoboxValue(infobox, "1weight"),
                GetInfoboxValue(infobox, "2weight"),
                GetInfoboxValue(infobox, "3weight"));
        }

        private static string GetCorpseName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name")
                   ?? article.Title;
        }

        private static string? BuildCorpseSummary(
            IReadOnlyDictionary<string, string>? infobox,
            string? storedSummary)
        {
            string? summary = NormalizeCorpseValue(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            string? corpseOf = GetInfoboxValue(infobox, "corpseof");
            string? flavourText = GetInfoboxValue(infobox, "flavortext");
            string? notes = GetInfoboxValue(infobox, "notes");
            string? product = GetInfoboxValue(infobox, "product");
            string? liquid = GetInfoboxValue(infobox, "liquid");
            string? skinable = GetInfoboxValue(infobox, "skinable");

            List<string> parts = [];

            if(!string.IsNullOrWhiteSpace(corpseOf))
            {
                parts.Add($"Corpse of {corpseOf}.");
            }

            if(string.Equals(skinable, "yes", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add("Skinable.");
            }

            if(!string.IsNullOrWhiteSpace(product))
            {
                parts.Add($"Product: {product}.");
            }
            else if(!string.IsNullOrWhiteSpace(liquid))
            {
                parts.Add($"Liquid: {liquid}.");
            }

            if(!string.IsNullOrWhiteSpace(flavourText))
            {
                parts.Add(flavourText);
            }
            else if(!string.IsNullOrWhiteSpace(notes))
            {
                parts.Add(notes);
            }

            if(parts.Count == 0)
            {
                return null;
            }

            string combined = string.Join(" ", parts).Trim();
            return combined.Length <= 280
            ? combined
            : combined[..277].TrimEnd() + "...";
        }

        private static string? GetInfoboxValue(IReadOnlyDictionary<string, string>? infobox, string key)
        {
            if(infobox is null ||
               !infobox.TryGetValue(key, out string? value))
            {
                return null;
            }

            return NormalizeCorpseValue(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeCorpseValue(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Replace("\r", " ")
                                     .Replace("\n", " ")
                                     .Replace("\\n", " ")
                                     .Replace("<br />", " ", StringComparison.OrdinalIgnoreCase)
                                     .Replace("<br/>", " ", StringComparison.OrdinalIgnoreCase)
                                     .Replace("<br>", " ", StringComparison.OrdinalIgnoreCase)
                                     .Trim();

            while (normalized.Contains("  ", StringComparison.Ordinal))
            {
                normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
            }

            if(string.IsNullOrWhiteSpace(normalized) ||
               string.Equals(normalized, "?", StringComparison.Ordinal) ||
               string.Equals(normalized, "}}", StringComparison.Ordinal))
            {
                return null;
            }

            return normalized;
        }
    }
}
