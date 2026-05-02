using System.Globalization;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Buildings;
using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Buildings.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Buildings
{
    public sealed class BuildingsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IBuildingsDataBaseService
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

        public async Task<IReadOnlyList<BuildingListItemResponse>> GetBuildingsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "buildings:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Building)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseBuildingInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsBuildingDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapBuildingListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Buildings],
                cancellationToken);
        }

        public async Task<IReadOnlyList<BuildingListItemResponse>?> GetBuildingsByCityAsync(string city, CancellationToken cancellationToken = default)
        {
            string normalizedCity = EntityNameNormalizer.Normalize(city);
            if(string.IsNullOrWhiteSpace(normalizedCity))
            {
                return [];
            }

            string cacheKey = ($"buildings:list:city:{normalizedCity}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Building)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseBuildingInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsBuildingDetailArticle(x.Article, x.Infobox))
                                   .Where(x => EntityNameNormalizer.NormalizeOptional(GetInfoboxValue(x.Infobox, "city")) == normalizedCity)
                                   .Select(x => MapBuildingListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Buildings],
                cancellationToken);
        }
        public async Task<BuildingDetailsResponse?> GetBuildingDetailsByNameAsync(string buildingName, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(buildingName))
            {
                return null;
            }

            string normalizedName = EntityNameNormalizer.Normalize(buildingName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"buildings:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int buildingId = await db.WikiArticles
                                             .AsNoTracking()
                                             .Where(x => x.ContentType == WikiContentType.Building)
                                             .Where(x => !x.IsMissingFromSource)
                                             .Where(x => x.NormalizedTitle == normalizedName)
                                             .Select(x => x.Id)
                                             .FirstOrDefaultAsync(ct);

                    if(buildingId <= 0)
                    {
                        return null;
                    }

                    return await GetBuildingDetailsByIdAsync(buildingId, ct);
                },
                _cacheOptions,
                [CacheTags.Buildings],
                cancellationToken);
        }

        public async Task<BuildingDetailsResponse?> GetBuildingDetailsByIdAsync(int buildingId, CancellationToken cancellationToken = default)
        {
            if(buildingId <= 0)
            {
                return null;
            }

            string cacheKey = ($"buildings:by-id:{buildingId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Building)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == buildingId)
                                                   .FirstOrDefaultAsync(ct);

                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseBuildingInfobox(article.InfoboxJson);
                    if(!IsBuildingDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapBuildingDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Buildings],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetBuildingSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "buildings:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Building)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseBuildingInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsBuildingDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(
                                       x.Article.Id,
                                       x.Article.LastUpdated,
                                       x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Buildings],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetBuildingSyncStatesSinceAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"buildings:sync-states-since:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Building)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseBuildingInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsBuildingDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(
                                       x.Article.Id,
                                       x.Article.LastUpdated,
                                       x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Buildings],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseBuildingInfobox(string? infoboxJson)
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

        private static bool IsBuildingDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(!string.Equals(article.InfoboxTemplate, "Infobox Building", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static BuildingListItemResponse MapBuildingListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new BuildingListItemResponse(
                article.Id,
                GetBuildingName(article, infobox),
                BuildBuildingSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static BuildingDetailsResponse MapBuildingDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new BuildingDetailsResponse(
                article.Id,
                GetBuildingName(article, infobox),
                BuildBuildingSummary(infobox, article.Summary),
                GetInfoboxValue(infobox, "type"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "city"),
                GetInfoboxValue(infobox, "location"),
                GetInfoboxValue(infobox, "street"),
                GetInfoboxValue(infobox, "street2"),
                GetInfoboxValue(infobox, "street3"),
                GetInfoboxValue(infobox, "houseid"),
                GetInfoboxValue(infobox, "size"),
                GetInfoboxValue(infobox, "beds"),
                GetInfoboxValue(infobox, "rent"),
                GetInfoboxValue(infobox, "openwindows"),
                GetInfoboxValue(infobox, "floors"),
                GetInfoboxValue(infobox, "rooms"),
                GetInfoboxValue(infobox, "furnishings"),
                GetInfoboxValue(infobox, "history"),
                GetInfoboxValue(infobox, "notes"),
                GetInfoboxValue(infobox, "ownable"),
                GetInfoboxValue(infobox, "posx"),
                GetInfoboxValue(infobox, "posy"),
                GetInfoboxValue(infobox, "posz"),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static BuildingStructuredDataResponse? CreateStructuredData(
            WikiArticle article,
            IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) &&
               (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new BuildingStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapBuildingInfobox(infobox),
                BuildBuildingAddresses(infobox),
                BuildBuildingCoordinates(infobox));
        }

        private static string GetBuildingName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name")
                   ?? article.Title;
        }

        private static string? BuildBuildingSummary(
            IReadOnlyDictionary<string, string>? infobox,
            string? storedSummary)
        {
            string? summary = NormalizeSummary(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            string? type = GetInfoboxValue(infobox, "type");
            string? city = GetInfoboxValue(infobox, "city");
            string? location = NormalizeSummary(GetInfoboxValue(infobox, "location"));
            string? notes = NormalizeSummary(GetInfoboxValue(infobox, "notes"));

            List<string> parts = [];

            if(!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(city))
            {
                parts.Add($"{type} in {city}.");
            }
            else if(!string.IsNullOrWhiteSpace(type))
            {
                parts.Add(type);
            }
            else if(!string.IsNullOrWhiteSpace(city))
            {
                parts.Add(city);
            }

            if(!string.IsNullOrWhiteSpace(location))
            {
                parts.Add(location);
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
               !infobox.TryGetValue(key, out string? value) ||
               string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static BuildingInfoboxResponse? MapBuildingInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new BuildingInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "type"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "city"),
                GetInfoboxValue(infobox, "location"),
                GetInfoboxValue(infobox, "street"),
                GetInfoboxValue(infobox, "street2"),
                GetInfoboxValue(infobox, "street3"),
                GetInfoboxValue(infobox, "houseid"),
                GetInfoboxValue(infobox, "size"),
                GetInfoboxValue(infobox, "beds"),
                GetInfoboxValue(infobox, "rent"),
                GetInfoboxValue(infobox, "openwindows"),
                GetInfoboxValue(infobox, "floors"),
                GetInfoboxValue(infobox, "rooms"),
                GetInfoboxValue(infobox, "furnishings"),
                GetInfoboxValue(infobox, "history"),
                GetInfoboxValue(infobox, "notes"),
                GetInfoboxValue(infobox, "ownable"),
                GetInfoboxValue(infobox, "posx"),
                GetInfoboxValue(infobox, "posy"),
                GetInfoboxValue(infobox, "posz"),
                GetInfoboxValue(infobox, "image"),
                infobox);
        }

        private static IReadOnlyList<BuildingAddressResponse> BuildBuildingAddresses(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return [];
            }

            List<BuildingAddressResponse> addresses = [];
            string? city = GetInfoboxValue(infobox, "city");
            string? location = GetInfoboxValue(infobox, "location");

            for(int index = 1; index <= 5; index++)
            {
                string suffix = index == 1 ? string.Empty : index.ToString(CultureInfo.InvariantCulture);
                string? street = GetInfoboxValue(infobox, $"street{suffix}");

                if(string.IsNullOrWhiteSpace(street))
                {
                    continue;
                }

                addresses.Add(new BuildingAddressResponse(index, street, city, location));
            }

            return addresses;
        }

        private static BuildingCoordinatesResponse? BuildBuildingCoordinates(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            decimal? x = ParseDecimal(GetInfoboxValue(infobox, "posx"));
            decimal? y = ParseDecimal(GetInfoboxValue(infobox, "posy"));
            int? z = ParseInt(GetInfoboxValue(infobox, "posz"));

            if(x is null && y is null && z is null)
            {
                return null;
            }

            return new BuildingCoordinatesResponse(x, y, z);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeSummary(string? value)
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

            return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
        }

        private static decimal? ParseDecimal(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
                ? parsed
                : null;
        }

        private static int? ParseInt(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : null;
        }
    }
}
