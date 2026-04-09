using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Objects;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Objects.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Objects
{
    public sealed class ObjectsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IObjectsDataBaseService
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

        public async Task<IReadOnlyList<TibiaObjectListItemResponse>> GetObjectsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "objects:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Object)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseObjectInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsObjectDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapObjectListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Objects],
                cancellationToken);
        }

        public async Task<TibiaObjectDetailsResponse?> GetObjectDetailsByNameAsync(string objectName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(objectName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"objects:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int objectId = await db.WikiArticles
                                           .AsNoTracking()
                                           .Where(x => x.ContentType == WikiContentType.Object)
                                           .Where(x => !x.IsMissingFromSource)
                                           .Where(x => x.NormalizedTitle == normalizedName)
                                           .Select(x => x.Id)
                                           .FirstOrDefaultAsync(ct);

                    if(objectId <= 0)
                    {
                        return null;
                    }

                    return await GetObjectDetailsByIdAsync(objectId, ct);
                },
                _cacheOptions,
                [CacheTags.Objects],
                cancellationToken);
        }

        public async Task<TibiaObjectDetailsResponse?> GetObjectDetailsByIdAsync(int objectId, CancellationToken cancellationToken = default)
        {
            if(objectId <= 0)
            {
                return null;
            }

            string cacheKey = ($"objects:by-id:{objectId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Object)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == objectId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseObjectInfobox(article.InfoboxJson);
                    if(!IsObjectDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapObjectDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Objects],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetObjectSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "objects:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Object)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseObjectInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsObjectDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Objects],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetObjectSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"objects:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Object)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseObjectInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsObjectDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Objects],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseObjectInfobox(string? infoboxJson)
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

        private static bool IsObjectDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            return string.Equals(normalizedTemplate, "Infobox Object", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static TibiaObjectListItemResponse MapObjectListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new TibiaObjectListItemResponse(
                article.Id,
                GetObjectName(article, infobox),
                BuildObjectSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static TibiaObjectDetailsResponse MapObjectDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new TibiaObjectDetailsResponse(
                article.Id,
                GetObjectName(article, infobox),
                BuildObjectSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static TibiaObjectStructuredDataResponse? CreateStructuredData(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) && (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new TibiaObjectStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapObjectInfobox(infobox));
        }

        private static TibiaObjectInfoboxResponse? MapObjectInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new TibiaObjectInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "actualname"),
                GetInfoboxValue(infobox, "article"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "itemid"),
                GetInfoboxValue(infobox, "primarytype"),
                GetInfoboxValue(infobox, "secondarytype"),
                GetInfoboxValue(infobox, "objectclass"),
                GetInfoboxValue(infobox, "pickupable"),
                GetInfoboxValue(infobox, "immobile"),
                GetInfoboxValue(infobox, "walkable"),
                GetInfoboxValue(infobox, "droppedby"),
                GetInfoboxValue(infobox, "sellto"),
                GetInfoboxValue(infobox, "buyfrom"),
                GetInfoboxValue(infobox, "npcprice"),
                GetInfoboxValue(infobox, "npcvalue"),
                GetInfoboxValue(infobox, "value"),
                GetInfoboxValue(infobox, "weight"),
                GetInfoboxValue(infobox, "marketable"),
                GetInfoboxValue(infobox, "stackable"),
                GetInfoboxValue(infobox, "notes"),
                infobox);
        }

        private static string GetObjectName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name") ?? article.Title;
        }

        private static string? BuildObjectSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? summary = NormalizeObjectText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            List<string> parts = [];
            string? primaryType = GetInfoboxValue(infobox, "primarytype");
            string? objectClass = GetInfoboxValue(infobox, "objectclass");
            string? value = GetInfoboxValue(infobox, "value");
            string? notes = GetInfoboxValue(infobox, "notes");

            if(!string.IsNullOrWhiteSpace(primaryType))
            {
                parts.Add($"{primaryType}.");
            }

            if(!string.IsNullOrWhiteSpace(objectClass))
            {
                parts.Add($"Class: {objectClass}.");
            }

            if(!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"Value: {value}.");
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

        private static string? GetInfoboxValue(IReadOnlyDictionary<string, string>? infobox, string key)
        {
            if(infobox is null || !infobox.TryGetValue(key, out string? value))
            {
                return null;
            }

            return NormalizeObjectText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeObjectText(string? value)
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
