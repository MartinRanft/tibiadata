using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Streets;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Streets.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Streets
{
    public sealed class StreetsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IStreetsDataBaseService
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

        public async Task<IReadOnlyList<StreetListItemResponse>> GetStreetsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "streets:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Street)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseStreetInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsStreetDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapStreetListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Streets],
                cancellationToken);
        }

        public async Task<StreetDetailsResponse?> GetStreetDetailsByNameAsync(string streetName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(streetName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"streets:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int streetId = await db.WikiArticles
                                           .AsNoTracking()
                                           .Where(x => x.ContentType == WikiContentType.Street)
                                           .Where(x => !x.IsMissingFromSource)
                                           .Where(x => x.NormalizedTitle == normalizedName)
                                           .Select(x => x.Id)
                                           .FirstOrDefaultAsync(ct);

                    if(streetId <= 0)
                    {
                        return null;
                    }

                    return await GetStreetDetailsByIdAsync(streetId, ct);
                },
                _cacheOptions,
                [CacheTags.Streets],
                cancellationToken);
        }

        public async Task<StreetDetailsResponse?> GetStreetDetailsByIdAsync(int streetId, CancellationToken cancellationToken = default)
        {
            if(streetId <= 0)
            {
                return null;
            }

            string cacheKey = ($"streets:by-id:{streetId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Street)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == streetId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseStreetInfobox(article.InfoboxJson);
                    if(!IsStreetDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapStreetDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Streets],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetStreetSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "streets:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Street)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseStreetInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsStreetDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Streets],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetStreetSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"streets:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Street)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseStreetInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsStreetDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Streets],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseStreetInfobox(string? infoboxJson)
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

        private static bool IsStreetDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            return string.Equals(normalizedTemplate, "Infobox Street", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static StreetListItemResponse MapStreetListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new StreetListItemResponse(
                article.Id,
                GetStreetName(article, infobox),
                BuildStreetSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static StreetDetailsResponse MapStreetDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new StreetDetailsResponse(
                article.Id,
                GetStreetName(article, infobox),
                BuildStreetSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static StreetStructuredDataResponse? CreateStructuredData(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) && (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new StreetStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapStreetInfobox(infobox));
        }

        private static StreetInfoboxResponse? MapStreetInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new StreetInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "actualname"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "city"),
                GetInfoboxValue(infobox, "city2"),
                GetInfoboxValue(infobox, "floor"),
                GetInfoboxValue(infobox, "map"),
                GetInfoboxValue(infobox, "style"),
                GetInfoboxValue(infobox, "notes"),
                infobox);
        }

        private static string GetStreetName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name") ?? article.Title;
        }

        private static string? BuildStreetSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? summary = NormalizeStreetText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            List<string> parts = [];
            string? city = GetInfoboxValue(infobox, "city") ?? GetInfoboxValue(infobox, "city2");
            string? style = GetInfoboxValue(infobox, "style");
            string? floor = GetInfoboxValue(infobox, "floor");
            string? notes = GetInfoboxValue(infobox, "notes");

            if(!string.IsNullOrWhiteSpace(city))
            {
                parts.Add($"City: {city}.");
            }

            if(!string.IsNullOrWhiteSpace(style))
            {
                parts.Add($"Style: {style}.");
            }

            if(!string.IsNullOrWhiteSpace(floor))
            {
                parts.Add($"Floor: {floor}.");
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

            return NormalizeStreetText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeStreetText(string? value)
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
