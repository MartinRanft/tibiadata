using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Bosstiary;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Bosstiary.Interfaces;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.DataBaseService.Bosstiary
{
    public sealed class BosstiaryDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IBosstiaryDataBaseService
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

        public async Task<IReadOnlyList<BosstiaryCategoryResponse>> GetBosstiaryCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "bosstiary:categories",
                async ct =>
                {
                    IReadOnlyList<BosstiaryCreatureSource> sources = await LoadBosstiaryCreatureSourcesAsync(ct);
                    Dictionary<string, int> creatureCounts = sources.GroupBy(entry => entry.CategorySlug, StringComparer.OrdinalIgnoreCase)
                                                                    .ToDictionary(
                                                                        group => group.Key,
                                                                        group => group.Count(),
                                                                        StringComparer.OrdinalIgnoreCase);

                    return BosstiaryCatalog.Categories
                                           .OrderBy(entry => entry.SortOrder)
                                           .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                                           .Select(entry => MapBosstiaryCategory(
                                               entry,
                                               creatureCounts.GetValueOrDefault(entry.Slug, 0)))
                                           .ToList();
                },
                _cacheOptions,
                [CacheTags.Bosstiary],
                cancellationToken);
        }

        public async Task<BosstiaryCategoryResponse?> GetBosstiaryCategoryBySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(categorySlug))
            {
                return null;
            }

            string? normalizedSlug = NormalizeBosstiarySlug(categorySlug);

            if(string.IsNullOrWhiteSpace(normalizedSlug))
            {
                return null;
            }

            string cacheKey = $"bosstiary:category:{normalizedSlug}".ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IReadOnlyList<BosstiaryCategoryResponse> categories = await GetBosstiaryCategoriesAsync(ct);

                    return categories.FirstOrDefault(entry =>
                        string.Equals(entry.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));
                },
                _cacheOptions,
                [CacheTags.Bosstiary],
                cancellationToken);
        }

        public async Task<BosstiaryCategoryResponse?> GetBosstiaryCategoryByIdAsync(int categoryId, CancellationToken cancellationToken = default)
        {
            if(categoryId <= 0)
            {
                return null;
            }

            string cacheKey = $"bosstiary:category:{categoryId}".ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IReadOnlyList<BosstiaryCategoryResponse> categories = await GetBosstiaryCategoriesAsync(ct);

                    return categories.FirstOrDefault(entry => entry.Id == categoryId);
                },
                _cacheOptions,
                [CacheTags.Bosstiary],
                cancellationToken);
        }

        public async Task<BosstiaryCategoryCreaturesResponse?> GetBosstiaryCreaturesByCategoryAsync(string categorySlug, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(categorySlug))
            {
                return null;
            }

            string? normalizedSlug = NormalizeBosstiarySlug(categorySlug);

            if(string.IsNullOrWhiteSpace(normalizedSlug))
            {
                return null;
            }

            BosstiaryCategoryDefinition? categoryDefinition = BosstiaryCatalog.Categories.FirstOrDefault(entry =>
                string.Equals(entry.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));

            if(categoryDefinition is null)
            {
                return null;
            }

            string cacheKey = $"bosstiary:category-creatures:{normalizedSlug}".ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IReadOnlyList<BosstiaryCreatureSource> sources = await LoadBosstiaryCreatureSourcesAsync(ct);

                    List<BosstiaryCreatureSource> filteredSources = sources
                                                                    .Where(entry => string.Equals(
                                                                        entry.CategorySlug,
                                                                        normalizedSlug,
                                                                        StringComparison.OrdinalIgnoreCase))
                                                                    .OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
                                                                    .ToList();

                    return MapBosstiaryCategoryCreatures(categoryDefinition, filteredSources);
                },
                _cacheOptions,
                [CacheTags.Bosstiary],
                cancellationToken);
        }

        public async Task<IReadOnlyList<BosstiaryPointOverviewItemResponse>> GetBosstiaryPointOverviewAsync(string? sortBy = null, bool descending = false, CancellationToken cancellationToken = default)
        {
            string normalizedSortBy = NormalizeSortField(sortBy, "points");
            string cacheKey = $"bosstiary:points:{normalizedSortBy}:{descending}".ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IReadOnlyList<BosstiaryCreatureSource> sources = await LoadBosstiaryCreatureSourcesAsync(ct);

                    List<BosstiaryPointOverviewItemResponse> items = sources.Select(MapBosstiaryPointOverviewItem)
                                                                             .ToList();

                    return SortPointOverview(items, normalizedSortBy, descending);
                },
                _cacheOptions,
                [CacheTags.Bosstiary],
                cancellationToken);
        }

        public async Task<BosstiaryFilterOptionsResponse> GetBosstiaryFilterOptionsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "bosstiary:filter-options",
                async ct =>
                {
                    IReadOnlyList<BosstiaryCategoryResponse> categories = await GetBosstiaryCategoriesAsync(ct);
                    return MapBosstiaryFilterOptions(categories);
                },
                _cacheOptions,
                [CacheTags.Bosstiary],
                cancellationToken);
        }

        public async Task<BosstiaryFilteredCreaturesResponse> GetFilteredBosstiaryCreaturesAsync(string? categorySlug = null, int? totalPoints = null, string? search = null, string? sortBy = null, bool descending = false, int page = 1,
            int pageSize = 100, CancellationToken cancellationToken = default)
        {
            string? normalizedCategorySlug = NormalizeBosstiarySlug(categorySlug);
            string normalizedSortBy = NormalizeSortField(sortBy, "name");
            int sanitizedPage = Math.Max(1, page);
            int sanitizedPageSize = Math.Clamp(pageSize, 1, 250);
            string? normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            string cacheKey =
                $"bosstiary:filtered:{normalizedCategorySlug}:{totalPoints}:{normalizedSearch}:{normalizedSortBy}:{descending}:{sanitizedPage}:{sanitizedPageSize}"
                .ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IReadOnlyList<BosstiaryCreatureSource> allSources = await LoadBosstiaryCreatureSourcesAsync(ct);

                    IEnumerable<BosstiaryCreatureSource> query = allSources;

                    if(!string.IsNullOrWhiteSpace(normalizedCategorySlug))
                    {
                        query = query.Where(entry => string.Equals(entry.CategorySlug, normalizedCategorySlug, StringComparison.OrdinalIgnoreCase));
                    }

                    if(totalPoints is > 0)
                    {
                        query = query.Where(entry =>
                        {
                            BosstiaryCategoryDefinition definition = GetRequiredCategoryDefinition(entry.CategorySlug);
                            return GetTotalPoints(entry, definition) == totalPoints.Value;
                        });
                    }

                    if(!string.IsNullOrWhiteSpace(normalizedSearch))
                    {
                        query = query.Where(entry => entry.CreatureName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
                    }

                    List<BosstiaryCreatureSource> filteredSources = query.ToList();
                    int totalCount = filteredSources.Count;

                    filteredSources = SortCreatureSources(filteredSources, normalizedSortBy, descending)
                                     .Skip((sanitizedPage - 1) * sanitizedPageSize)
                                     .Take(sanitizedPageSize)
                                     .ToList();

                    return MapFilteredCreatures(
                        normalizedCategorySlug,
                        totalPoints,
                        normalizedSearch,
                        normalizedSortBy,
                        descending,
                        sanitizedPage,
                        sanitizedPageSize,
                        totalCount,
                        filteredSources);
                },
                _cacheOptions,
                [CacheTags.Bosstiary],
                cancellationToken);
        }

        private async Task<IReadOnlyList<BosstiaryCreatureSource>> LoadBosstiaryCreatureSourcesAsync(CancellationToken cancellationToken)
        {
            List<BosstiaryCreatureRecord> records = await db.Creatures
                                                            .AsNoTracking()
                                                            .Where(entry => entry.BestiaryJson != null && entry.BestiaryJson != string.Empty)
                                                            .OrderBy(entry => entry.Name)
                                                            .Select(entry => new BosstiaryCreatureRecord(
                                                                entry.Id,
                                                                entry.Name,
                                                                entry.LastUpdated,
                                                                entry.BestiaryJson))
                                                            .ToListAsync(cancellationToken);

            return records.Select(entry => ParseBosstiaryCreatureSource(
                                                   entry.Id,
                                                   entry.Name,
                                                   entry.LastUpdated,
                                                   entry.BestiaryJson))
                          .Where(entry => entry is not null)
                          .Select(entry => entry!)
                          .OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
                          .ToList();
        }

        private static BosstiaryCategoryResponse MapBosstiaryCategory(
            BosstiaryCategoryDefinition definition,
            int creatureCount)
        {
            return new BosstiaryCategoryResponse(
                definition.SortOrder,
                definition.Name,
                definition.Slug,
                definition.SortOrder,
                definition.GetTotalPoints(),
                definition.GetTotalKillsRequired(),
                creatureCount);
        }

        private static BosstiaryCreatureListItemResponse MapBosstiaryCreatureListItem(BosstiaryCreatureSource source)
        {
            BosstiaryCategoryDefinition categoryDefinition = GetRequiredCategoryDefinition(source.CategorySlug);
            IReadOnlyList<BosstiaryLevelRequirementResponse> levelRequirements = GetLevelRequirements(source, categoryDefinition);
            int totalKillsRequired = GetTotalKillsRequired(source, categoryDefinition, levelRequirements);

            return new BosstiaryCreatureListItemResponse(
                source.CreatureId,
                source.CreatureName,
                categoryDefinition.Name,
                categoryDefinition.Slug,
                categoryDefinition.SortOrder,
                GetTotalPoints(source, categoryDefinition),
                totalKillsRequired,
                levelRequirements,
                source.LastUpdated);
        }

        private static BosstiaryPointOverviewItemResponse MapBosstiaryPointOverviewItem(BosstiaryCreatureSource source)
        {
            BosstiaryCategoryDefinition categoryDefinition = GetRequiredCategoryDefinition(source.CategorySlug);
            IReadOnlyList<BosstiaryLevelRequirementResponse> levelRequirements = GetLevelRequirements(source, categoryDefinition);

            return new BosstiaryPointOverviewItemResponse(
                source.CreatureId,
                source.CreatureName,
                categoryDefinition.Name,
                categoryDefinition.SortOrder,
                GetTotalPoints(source, categoryDefinition),
                GetTotalKillsRequired(source, categoryDefinition, levelRequirements),
                source.LastUpdated);
        }

        private static BosstiaryCategoryCreaturesResponse MapBosstiaryCategoryCreatures(
            BosstiaryCategoryDefinition categoryDefinition,
            IReadOnlyList<BosstiaryCreatureSource> sources)
        {
            IReadOnlyList<BosstiaryLevelRequirementResponse> levelRequirements =
                MapLevelRequirements(categoryDefinition.LevelRequirements);

            List<BosstiaryCreatureListItemResponse> creatures = sources.Select(MapBosstiaryCreatureListItem)
                                                                       .OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
                                                                       .ToList();

            return new BosstiaryCategoryCreaturesResponse(
                categoryDefinition.Name,
                categoryDefinition.Slug,
                categoryDefinition.SortOrder,
                categoryDefinition.GetTotalPoints(),
                categoryDefinition.GetTotalKillsRequired(),
                levelRequirements,
                creatures.Count,
                creatures);
        }

        private static BosstiaryFilterOptionsResponse MapBosstiaryFilterOptions(
            IReadOnlyList<BosstiaryCategoryResponse> categories)
        {
            return new BosstiaryFilterOptionsResponse(categories);
        }

        private static BosstiaryFilteredCreaturesResponse MapFilteredCreatures(
            string? categorySlug,
            int? totalPoints,
            string? search,
            string? sortBy,
            bool descending,
            int page,
            int pageSize,
            int totalCount,
            IReadOnlyList<BosstiaryCreatureSource> sources)
        {
            return new BosstiaryFilteredCreaturesResponse(
                NormalizeBosstiarySlug(categorySlug),
                totalPoints,
                string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.Trim(),
                descending,
                page,
                pageSize,
                totalCount,
                sources.Select(MapBosstiaryCreatureListItem).ToList());
        }

        private static List<BosstiaryPointOverviewItemResponse> SortPointOverview(
            List<BosstiaryPointOverviewItemResponse> items,
            string sortBy,
            bool descending)
        {
            IOrderedEnumerable<BosstiaryPointOverviewItemResponse> ordered = sortBy switch
            {
                "name" => items.OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "category" => items.OrderBy(entry => entry.CategorySortOrder)
                                   .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "updated" => items.OrderBy(entry => entry.LastUpdated)
                                  .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "kills" => items.OrderBy(entry => entry.TotalKillsRequired)
                                .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                _ => items.OrderBy(entry => entry.TotalPoints)
                          .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
            };

            return descending ? ordered.Reverse().ToList() : ordered.ToList();
        }

        private static List<BosstiaryCreatureSource> SortCreatureSources(
            List<BosstiaryCreatureSource> sources,
            string sortBy,
            bool descending)
        {
            IOrderedEnumerable<BosstiaryCreatureSource> ordered = sortBy switch
            {
                "category" => sources.OrderBy(entry => GetRequiredCategoryDefinition(entry.CategorySlug).SortOrder)
                                     .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "points" => sources.OrderBy(entry => GetTotalPoints(entry, GetRequiredCategoryDefinition(entry.CategorySlug)))
                                   .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "kills" => sources.OrderBy(entry =>
                                   {
                                       BosstiaryCategoryDefinition definition = GetRequiredCategoryDefinition(entry.CategorySlug);
                                       IReadOnlyList<BosstiaryLevelRequirementResponse> levels = GetLevelRequirements(entry, definition);
                                       return GetTotalKillsRequired(entry, definition, levels);
                                   })
                                   .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "updated" => sources.OrderBy(entry => entry.LastUpdated)
                                    .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                _ => sources.OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
            };

            return descending ? ordered.Reverse().ToList() : ordered.ToList();
        }

        private static string NormalizeSortField(string? sortBy, string fallback)
        {
            string normalized = NormalizeBosstiarySlug(sortBy) ?? string.Empty;

            return normalized switch
            {
                "name" => "name",
                "category" => "category",
                "points" => "points",
                "totalpoints" => "points",
                "total-points" => "points",
                "kills" => "kills",
                "totalkillsrequired" => "kills",
                "total-kills-required" => "kills",
                "updated" => "updated",
                "lastupdated" => "updated",
                "last-updated" => "updated",
                _ => fallback
            };
        }

        private static BosstiaryCreatureSource? ParseBosstiaryCreatureSource(
            int creatureId,
            string creatureName,
            DateTime lastUpdated,
            string? bestiaryJson)
        {
            if(string.IsNullOrWhiteSpace(bestiaryJson))
            {
                return null;
            }

            try
            {
                Dictionary<string, JsonElement>? values =
                    JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bestiaryJson, JsonOptions);

                if(values is null)
                {
                    return null;
                }

                string? categorySlug = GetJsonString(values, "bosstiaryCategorySlug") ??
                                       GetJsonString(values, "bosstiaryCategory") ??
                                       GetJsonString(values, "bosstiaryClass") ??
                                       GetJsonString(values, "bosstiaryclass");

                if(string.IsNullOrWhiteSpace(categorySlug))
                {
                    return null;
                }

                string? normalizedCategorySlug = NormalizeBosstiarySlug(categorySlug);

                if(string.IsNullOrWhiteSpace(normalizedCategorySlug) || !IsKnownBosstiaryCategory(normalizedCategorySlug))
                {
                    return null;
                }

                List<BosstiaryLevelRequirementResponse>? levelRequirements = ParseLevelRequirements(values);

                return new BosstiaryCreatureSource(
                    creatureId,
                    creatureName,
                    normalizedCategorySlug,
                    GetJsonInt32(values, "bosstiaryPoints") ??
                    GetJsonInt32(values, "totalPoints"),
                    GetJsonInt32(values, "bosstiaryTotalKillsRequired") ??
                    GetJsonInt32(values, "totalKillsRequired"),
                    levelRequirements,
                    lastUpdated);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static BosstiaryCategoryDefinition GetRequiredCategoryDefinition(string categorySlug)
        {
            return BosstiaryCatalog.GetRequiredCategory(NormalizeBosstiarySlug(categorySlug)!);
        }

        private static bool IsKnownBosstiaryCategory(string slug)
        {
            return BosstiaryCatalog.Categories.Any(entry =>
                string.Equals(entry.Slug, slug, StringComparison.OrdinalIgnoreCase));
        }

        private static int GetTotalPoints(
            BosstiaryCreatureSource source,
            BosstiaryCategoryDefinition categoryDefinition)
        {
            return source.TotalPoints is > 0
                ? source.TotalPoints.Value
                : categoryDefinition.GetTotalPoints();
        }

        private static int GetTotalKillsRequired(
            BosstiaryCreatureSource source,
            BosstiaryCategoryDefinition categoryDefinition,
            IReadOnlyList<BosstiaryLevelRequirementResponse> levelRequirements)
        {
            return source.TotalKillsRequired is > 0
                ? source.TotalKillsRequired.Value
                : levelRequirements.Count > 0
                    ? levelRequirements.Sum(entry => entry.KillsRequired)
                    : categoryDefinition.GetTotalKillsRequired();
        }

        private static IReadOnlyList<BosstiaryLevelRequirementResponse> GetLevelRequirements(
            BosstiaryCreatureSource source,
            BosstiaryCategoryDefinition categoryDefinition)
        {
            return source.LevelRequirements is { Count: > 0 }
                ? source.LevelRequirements
                : MapLevelRequirements(categoryDefinition.LevelRequirements);
        }

        private static IReadOnlyList<BosstiaryLevelRequirementResponse> MapLevelRequirements(
            IReadOnlyList<BosstiaryLevelDefinition> levelRequirements)
        {
            return levelRequirements.Select(entry => new BosstiaryLevelRequirementResponse(
                                        entry.Level,
                                        entry.Name,
                                        entry.KillsRequired,
                                        entry.PointsAwarded))
                                    .ToList();
        }

        private static List<BosstiaryLevelRequirementResponse>? ParseLevelRequirements(
            IReadOnlyDictionary<string, JsonElement> values)
        {
            string[] levelRequirementKeys = ["bosstiaryLevelRequirements", "levelRequirements", "levels", "killStages"];

            foreach(string key in levelRequirementKeys)
            {
                if(!values.TryGetValue(key, out JsonElement element) || element.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                List<BosstiaryLevelRequirementResponse> entries = [];

                foreach(JsonElement levelElement in element.EnumerateArray())
                {
                    if(levelElement.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    int level = GetJsonInt32(levelElement, "level") ?? (entries.Count + 1);
                    string levelName = GetJsonString(levelElement, "name") ?? $"Level {level}";
                    int killsRequired = GetJsonInt32(levelElement, "killsRequired") ??
                                        GetJsonInt32(levelElement, "kills") ??
                                        GetJsonInt32(levelElement, "requiredKills") ??
                                        0;
                    int pointsAwarded = GetJsonInt32(levelElement, "pointsAwarded") ??
                                        GetJsonInt32(levelElement, "points") ??
                                        0;

                    if(killsRequired <= 0)
                    {
                        continue;
                    }

                    entries.Add(new BosstiaryLevelRequirementResponse(level, levelName, killsRequired, Math.Max(0, pointsAwarded)));
                }

                return entries.Count == 0 ? null : entries;
            }

            return null;
        }

        private static string? NormalizeBosstiarySlug(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim()
                        .ToLowerInvariant()
                        .Replace('_', '-')
                        .Replace(' ', '-');
        }

        private static string? GetJsonString(IReadOnlyDictionary<string, JsonElement> values, string key)
        {
            return values.TryGetValue(key, out JsonElement element)
                ? GetJsonString(element)
                : null;
        }

        private static string? GetJsonString(JsonElement element, string key)
        {
            return element.TryGetProperty(key, out JsonElement property)
                ? GetJsonString(property)
                : null;
        }

        private static string? GetJsonString(JsonElement element)
        {
            return element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : null;
        }

        private static int? GetJsonInt32(IReadOnlyDictionary<string, JsonElement> values, string key)
        {
            return values.TryGetValue(key, out JsonElement element)
                ? GetJsonInt32(element)
                : null;
        }

        private static int? GetJsonInt32(JsonElement element, string key)
        {
            return element.TryGetProperty(key, out JsonElement property)
                ? GetJsonInt32(property)
                : null;
        }

        private static int? GetJsonInt32(JsonElement element)
        {
            if(element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int intValue))
            {
                return intValue;
            }

            if(element.ValueKind == JsonValueKind.String &&
               int.TryParse(element.GetString(), out int parsedValue))
            {
                return parsedValue;
            }

            return null;
        }

        private sealed record BosstiaryCreatureSource(
            int CreatureId,
            string CreatureName,
            string CategorySlug,
            int? TotalPoints,
            int? TotalKillsRequired,
            IReadOnlyList<BosstiaryLevelRequirementResponse>? LevelRequirements,
            DateTime LastUpdated);

        private sealed record BosstiaryCreatureRecord(
            int Id,
            string Name,
            DateTime LastUpdated,
            string? BestiaryJson);
    }
}
