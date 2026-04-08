using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Mounts;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Mounts.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Mounts
{
    public sealed class MountsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IMountsDataBaseService
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

        public async Task<IReadOnlyList<MountListItemResponse>> GetMountsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "mounts:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Mount)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseMountInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsMountDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapMountListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Mounts],
                cancellationToken);
        }

        public async Task<MountDetailsResponse?> GetMountDetailsByNameAsync(string mountName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(mountName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"mounts:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int mountId = await db.WikiArticles
                                          .AsNoTracking()
                                          .Where(x => x.ContentType == WikiContentType.Mount)
                                          .Where(x => !x.IsMissingFromSource)
                                          .Where(x => x.NormalizedTitle == normalizedName)
                                          .Select(x => x.Id)
                                          .FirstOrDefaultAsync(ct);

                    if(mountId <= 0)
                    {
                        return null;
                    }

                    return await GetMountDetailsByIdAsync(mountId, ct);
                },
                _cacheOptions,
                [CacheTags.Mounts],
                cancellationToken);
        }

        public async Task<MountDetailsResponse?> GetMountDetailsByIdAsync(int mountId, CancellationToken cancellationToken = default)
        {
            if(mountId <= 0)
            {
                return null;
            }

            string cacheKey = ($"mounts:by-id:{mountId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Mount)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == mountId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseMountInfobox(article.InfoboxJson);
                    if(!IsMountDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapMountDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Mounts],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetMountSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "mounts:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Mount)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseMountInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsMountDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Mounts],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetMountSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"mounts:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Mount)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseMountInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsMountDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Mounts],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseMountInfobox(string? infoboxJson)
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

        private static bool IsMountDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            return string.Equals(normalizedTemplate, "Infobox Mount", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static MountListItemResponse MapMountListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new MountListItemResponse(
                article.Id,
                GetMountName(article, infobox),
                BuildMountSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static MountDetailsResponse MapMountDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new MountDetailsResponse(
                article.Id,
                GetMountName(article, infobox),
                BuildMountSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static MountStructuredDataResponse? CreateStructuredData(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) && (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new MountStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapMountInfobox(infobox));
        }

        private static MountInfoboxResponse? MapMountInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new MountInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "actualname"),
                GetInfoboxValue(infobox, "mountid"),
                GetInfoboxValue(infobox, "tamingmethod"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "speed"),
                GetInfoboxValue(infobox, "bought"),
                GetInfoboxValue(infobox, "price"),
                GetInfoboxValue(infobox, "achievement"),
                GetInfoboxValue(infobox, "tournament"),
                GetInfoboxValue(infobox, "colourisable"),
                GetInfoboxValue(infobox, "artwork"),
                GetNotes(infobox),
                GetHistory(infobox),
                GetInfoboxValue(infobox, "lightcolor"),
                GetInfoboxValue(infobox, "lightradius"));
        }

        private static string GetMountName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name") ?? article.Title;
        }

        private static string? BuildMountSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? summary = NormalizeMountText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            List<string> parts = [];
            string? tamingMethod = GetInfoboxValue(infobox, "tamingmethod");
            string? speed = GetInfoboxValue(infobox, "speed");
            string? price = GetInfoboxValue(infobox, "price");
            string? bought = GetInfoboxValue(infobox, "bought");
            string? notes = GetNotes(infobox);

            if(!string.IsNullOrWhiteSpace(tamingMethod))
            {
                parts.Add($"Taming: {tamingMethod}.");
            }

            if(!string.IsNullOrWhiteSpace(speed))
            {
                parts.Add($"Speed: {speed}.");
            }

            if(!string.IsNullOrWhiteSpace(price) || !string.IsNullOrWhiteSpace(bought))
            {
                string availability = string.Join(" ",
                    new[]
                    {
                        bought, price
                    }.Where(static x => !string.IsNullOrWhiteSpace(x)));
                parts.Add(availability.EndsWith(".", StringComparison.Ordinal) ? availability : availability + ".");
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
            return combined.Length <= 280 ? combined : combined[..277].TrimEnd() + "...";
        }

        private static string? GetNotes(IReadOnlyDictionary<string, string>? infobox)
        {
            string? notes = NormalizeMountText(GetInfoboxValue(infobox, "notes"));
            if(string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            if(ExtractPrefixedValue(notes, "history") is not null)
            {
                return null;
            }

            return notes;
        }

        private static string? GetHistory(IReadOnlyDictionary<string, string>? infobox)
        {
            string? history = NormalizeMountText(GetInfoboxValue(infobox, "history"));
            if(!string.IsNullOrWhiteSpace(history))
            {
                return history;
            }

            string? notes = NormalizeMountText(GetInfoboxValue(infobox, "notes"));
            return ExtractPrefixedValue(notes, "history");
        }

        private static string? GetInfoboxValue(IReadOnlyDictionary<string, string>? infobox, string key)
        {
            if(infobox is null || !infobox.TryGetValue(key, out string? value))
            {
                return null;
            }

            return NormalizeMountText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeMountText(string? value)
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

            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static string? ExtractPrefixedValue(string? value, string key)
        {
            string? normalized = NormalizeMountText(value);
            if(string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            string prefix = $"| {key} =";
            if(!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string extracted = normalized[prefix.Length..].Trim();
            return string.IsNullOrWhiteSpace(extracted) ? null : extracted;
        }
    }
}