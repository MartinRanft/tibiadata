using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Outfits;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Outfits.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Outfits
{
    public sealed class OutfitsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IOutfitsDataBaseService
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

        public async Task<IReadOnlyList<OutfitListItemResponse>> GetOutfitsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "outfits:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Outfit)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseOutfitInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsOutfitDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapOutfitListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Outfits],
                cancellationToken);
        }

        public async Task<OutfitDetailsResponse?> GetOutfitDetailsByNameAsync(string outfitName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(outfitName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"outfits:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int outfitId = await db.WikiArticles
                                           .AsNoTracking()
                                           .Where(x => x.ContentType == WikiContentType.Outfit)
                                           .Where(x => !x.IsMissingFromSource)
                                           .Where(x => x.NormalizedTitle == normalizedName)
                                           .Select(x => x.Id)
                                           .FirstOrDefaultAsync(ct);

                    if(outfitId <= 0)
                    {
                        return null;
                    }

                    return await GetOutfitDetailsByIdAsync(outfitId, ct);
                },
                _cacheOptions,
                [CacheTags.Outfits],
                cancellationToken);
        }

        public async Task<OutfitDetailsResponse?> GetOutfitDetailsByIdAsync(int outfitId, CancellationToken cancellationToken = default)
        {
            if(outfitId <= 0)
            {
                return null;
            }

            string cacheKey = ($"outfits:by-id:{outfitId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Outfit)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == outfitId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseOutfitInfobox(article.InfoboxJson);
                    if(!IsOutfitDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapOutfitDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Outfits],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetOutfitSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "outfits:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Outfit)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseOutfitInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsOutfitDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Outfits],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetOutfitSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"outfits:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Outfit)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseOutfitInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsOutfitDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Outfits],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseOutfitInfobox(string? infoboxJson)
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

        private static bool IsOutfitDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            return string.Equals(normalizedTemplate, "Infobox Outfit", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static OutfitListItemResponse MapOutfitListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new OutfitListItemResponse(
                article.Id,
                GetOutfitName(article, infobox),
                BuildOutfitSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static OutfitDetailsResponse MapOutfitDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new OutfitDetailsResponse(
                article.Id,
                GetOutfitName(article, infobox),
                BuildOutfitSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static OutfitStructuredDataResponse? CreateStructuredData(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) && (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new OutfitStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapOutfitInfobox(infobox));
        }

        private static OutfitInfoboxResponse? MapOutfitInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new OutfitInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "outfit"),
                GetInfoboxValue(infobox, "primarytype"),
                GetInfoboxValue(infobox, "secondarytype"),
                GetInfoboxValue(infobox, "maleid"),
                GetInfoboxValue(infobox, "femaleid"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "addons"),
                GetInfoboxValue(infobox, "premium"),
                GetInfoboxValue(infobox, "artwork"),
                GetInfoboxValue(infobox, "bought"),
                GetInfoboxValue(infobox, "achievement"),
                GetInfoboxValue(infobox, "baseoutfitprice"),
                GetInfoboxValue(infobox, "fulloutfitprice"),
                GetInfoboxValue(infobox, "addon1price"),
                GetInfoboxValue(infobox, "addon2price"),
                GetNotes(infobox),
                GetHistory(infobox),
                GetStatus(infobox),
                infobox);
        }

        private static string GetOutfitName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name") ?? article.Title;
        }

        private static string? BuildOutfitSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? summary = NormalizeOutfitText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            List<string> parts = [];
            string? primaryType = GetInfoboxValue(infobox, "primarytype");
            string? secondaryType = GetInfoboxValue(infobox, "secondarytype");
            string? addons = GetInfoboxValue(infobox, "addons");
            string? notes = GetNotes(infobox);

            if(!string.IsNullOrWhiteSpace(primaryType) && !string.IsNullOrWhiteSpace(secondaryType))
            {
                parts.Add($"{primaryType} / {secondaryType}.");
            }
            else if(!string.IsNullOrWhiteSpace(primaryType))
            {
                parts.Add($"{primaryType}.");
            }

            if(!string.IsNullOrWhiteSpace(addons))
            {
                parts.Add($"Addons: {addons}.");
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
            string? notes = NormalizeOutfitText(GetInfoboxValue(infobox, "notes"));
            if(string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            if(ExtractPrefixedValue(notes, "history") is not null ||
               ExtractPrefixedValue(notes, "status") is not null)
            {
                return null;
            }

            return notes;
        }

        private static string? GetHistory(IReadOnlyDictionary<string, string>? infobox)
        {
            string? history = NormalizeOutfitText(GetInfoboxValue(infobox, "history"));
            if(!string.IsNullOrWhiteSpace(history))
            {
                string? misplacedStatus = ExtractPrefixedValue(history, "status");
                if(misplacedStatus is null)
                {
                    return history;
                }
            }

            string? notes = NormalizeOutfitText(GetInfoboxValue(infobox, "notes"));
            return ExtractPrefixedValue(notes, "history");
        }

        private static string? GetStatus(IReadOnlyDictionary<string, string>? infobox)
        {
            string? status = NormalizeOutfitText(GetInfoboxValue(infobox, "status"));
            if(!string.IsNullOrWhiteSpace(status))
            {
                return status;
            }

            string? history = NormalizeOutfitText(GetInfoboxValue(infobox, "history"));
            string? misplacedStatus = ExtractPrefixedValue(history, "status");
            if(!string.IsNullOrWhiteSpace(misplacedStatus))
            {
                return misplacedStatus;
            }

            string? notes = NormalizeOutfitText(GetInfoboxValue(infobox, "notes"));
            return ExtractPrefixedValue(notes, "status");
        }

        private static string? GetInfoboxValue(IReadOnlyDictionary<string, string>? infobox, string key)
        {
            if(infobox is null || !infobox.TryGetValue(key, out string? value))
            {
                return null;
            }

            return NormalizeOutfitText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeOutfitText(string? value)
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
            string? normalized = NormalizeOutfitText(value);
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
