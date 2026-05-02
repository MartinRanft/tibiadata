using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Missiles;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Missiles.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Missiles
{
    public sealed class MissilesDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IMissilesDataBaseService
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

        public async Task<IReadOnlyList<MissileListItemResponse>> GetMissilesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "missiles:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Missile)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseMissileInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsMissileDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapMissileListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Missiles],
                cancellationToken);
        }

        public async Task<MissileDetailsResponse?> GetMissileDetailsByNameAsync(string missileName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(missileName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"missiles:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int missileId = await db.WikiArticles
                                            .AsNoTracking()
                                            .Where(x => x.ContentType == WikiContentType.Missile)
                                            .Where(x => !x.IsMissingFromSource)
                                            .Where(x => x.NormalizedTitle == normalizedName)
                                            .Select(x => x.Id)
                                            .FirstOrDefaultAsync(ct);

                    if(missileId <= 0)
                    {
                        return null;
                    }

                    return await GetMissileDetailsByIdAsync(missileId, ct);
                },
                _cacheOptions,
                [CacheTags.Missiles],
                cancellationToken);
        }

        public async Task<MissileDetailsResponse?> GetMissileDetailsByIdAsync(int missileId, CancellationToken cancellationToken = default)
        {
            if(missileId <= 0)
            {
                return null;
            }

            string cacheKey = ($"missiles:by-id:{missileId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Missile)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == missileId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseMissileInfobox(article.InfoboxJson);
                    if(!IsMissileDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapMissileDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Missiles],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetMissileSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "missiles:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Missile)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseMissileInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsMissileDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Missiles],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetMissileSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"missiles:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Missile)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseMissileInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsMissileDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Missiles],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseMissileInfobox(string? infoboxJson)
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

        private static bool IsMissileDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            return string.Equals(normalizedTemplate, "Infobox Missile", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static MissileListItemResponse MapMissileListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new MissileListItemResponse(
                article.Id,
                GetMissileName(article, infobox),
                BuildMissileSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static MissileDetailsResponse MapMissileDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new MissileDetailsResponse(
                article.Id,
                GetMissileName(article, infobox),
                BuildMissileSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static MissileStructuredDataResponse? CreateStructuredData(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) && (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new MissileStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapMissileInfobox(infobox));
        }

        private static MissileInfoboxResponse? MapMissileInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new MissileInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "primarytype"),
                GetInfoboxValue(infobox, "secondarytype"),
                GetInfoboxValue(infobox, "shotby"),
                GetInfoboxValue(infobox, "missileid"),
                GetInfoboxValue(infobox, "lightradius"),
                GetInfoboxValue(infobox, "lightcolor"),
                GetInfoboxValue(infobox, "notes"),
                infobox);
        }

        private static string GetMissileName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name") ?? article.Title;
        }

        private static string? BuildMissileSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? summary = NormalizeMissileText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            List<string> parts = [];
            string? shotBy = GetInfoboxValue(infobox, "shotby");
            string? primaryType = GetInfoboxValue(infobox, "primarytype");
            string? secondaryType = GetInfoboxValue(infobox, "secondarytype");
            string? notes = GetInfoboxValue(infobox, "notes");

            if(!string.IsNullOrWhiteSpace(shotBy))
            {
                parts.Add($"Shot by {shotBy}.");
            }

            if(!string.IsNullOrWhiteSpace(primaryType) && !string.IsNullOrWhiteSpace(secondaryType))
            {
                parts.Add($"{primaryType} / {secondaryType}.");
            }
            else if(!string.IsNullOrWhiteSpace(primaryType))
            {
                parts.Add($"{primaryType}.");
            }

            if(!string.IsNullOrWhiteSpace(notes))
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

        private static string? GetInfoboxValue(IReadOnlyDictionary<string, string>? infobox, string key)
        {
            if(infobox is null || !infobox.TryGetValue(key, out string? value))
            {
                return null;
            }

            return NormalizeMissileText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeMissileText(string? value)
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
    }
}
