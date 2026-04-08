using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Locations;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Locations.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Locations
{
    public sealed class LocationsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : ILocationsDataBaseService
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

        public async Task<IReadOnlyList<LocationListItemResponse>> GetLocationsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "locations:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Location)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseLocationInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsLocationDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapLocationListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Locations],
                cancellationToken);
        }

        public async Task<LocationDetailsResponse?> GetLocationDetailsByNameAsync(string locationName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(locationName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"locations:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int locationId = await db.WikiArticles
                                             .AsNoTracking()
                                             .Where(x => x.ContentType == WikiContentType.Location)
                                             .Where(x => !x.IsMissingFromSource)
                                             .Where(x => x.NormalizedTitle == normalizedName)
                                             .Select(x => x.Id)
                                             .FirstOrDefaultAsync(ct);

                    if(locationId <= 0)
                    {
                        return null;
                    }

                    return await GetLocationDetailsByIdAsync(locationId, ct);
                },
                _cacheOptions,
                [CacheTags.Locations],
                cancellationToken);
        }

        public async Task<LocationDetailsResponse?> GetLocationDetailsByIdAsync(int locationId, CancellationToken cancellationToken = default)
        {
            if(locationId <= 0)
            {
                return null;
            }

            string cacheKey = ($"locations:by-id:{locationId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Location)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == locationId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseLocationInfobox(article.InfoboxJson);
                    if(!IsLocationDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapLocationDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Locations],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetLocationSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "locations:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Location)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseLocationInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsLocationDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Locations],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetLocationSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"locations:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Location)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseLocationInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsLocationDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Locations],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseLocationInfobox(string? infoboxJson)
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

        private static bool IsLocationDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            return string.Equals(normalizedTemplate, "Infobox Location", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static LocationListItemResponse MapLocationListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new LocationListItemResponse(
                article.Id,
                GetLocationName(article, infobox),
                BuildLocationSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static LocationDetailsResponse MapLocationDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new LocationDetailsResponse(
                article.Id,
                GetLocationName(article, infobox),
                BuildLocationSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static LocationStructuredDataResponse? CreateStructuredData(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) && (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new LocationStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapLocationInfobox(infobox));
        }

        private static LocationInfoboxResponse? MapLocationInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new LocationInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "ruler"),
                GetInfoboxValue(infobox, "population"),
                GetInfoboxValue(infobox, "organization"),
                GetInfoboxValue(infobox, "organizations"),
                GetInfoboxValue(infobox, "links"),
                GetInfoboxValue(infobox, "near"),
                GetInfoboxValue(infobox, "status"),
                GetInfoboxValue(infobox, "seealso"),
                GetInfoboxValue(infobox, "image"),
                GetInfoboxValue(infobox, "map"),
                GetInfoboxValue(infobox, "map2"),
                GetInfoboxValue(infobox, "map3"),
                GetInfoboxValue(infobox, "map4"),
                GetInfoboxValue(infobox, "map6"));
        }

        private static string GetLocationName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name") ?? article.Title;
        }

        private static string? BuildLocationSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? summary = NormalizeLocationText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            List<string> parts = [];
            string? ruler = GetInfoboxValue(infobox, "ruler");
            string? population = GetInfoboxValue(infobox, "population");
            string? near = GetInfoboxValue(infobox, "near");
            string? organization = GetInfoboxValue(infobox, "organization") ?? GetInfoboxValue(infobox, "organizations");

            if(!string.IsNullOrWhiteSpace(ruler))
            {
                parts.Add($"Ruler: {ruler}.");
            }

            if(!string.IsNullOrWhiteSpace(population))
            {
                parts.Add($"Population: {population}.");
            }

            if(!string.IsNullOrWhiteSpace(near))
            {
                parts.Add($"Near: {near}.");
            }
            else if(!string.IsNullOrWhiteSpace(organization))
            {
                parts.Add($"Organization: {organization}.");
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

            return NormalizeLocationText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeLocationText(string? value)
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