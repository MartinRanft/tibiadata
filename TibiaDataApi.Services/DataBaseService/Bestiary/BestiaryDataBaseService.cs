using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Bestiary;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Bestiary.Interfaces;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.DataBaseService.Bestiary
{
    public sealed class BestiaryDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IBestiaryDataBaseService
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

        public async Task<IReadOnlyList<BestiaryClassResponse>> GetBestiaryClassesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "bestiary:classes",
                async ct =>
                {
                    IReadOnlyList<BestiaryCreatureSource> sources = await LoadBestiaryCreatureSourcesAsync(ct);
                    Dictionary<string, int> creatureCounts = sources.GroupBy(entry => entry.ClassSlug, StringComparer.OrdinalIgnoreCase)
                                                                    .ToDictionary(
                                                                        group => group.Key,
                                                                        group => group.Count(),
                                                                        StringComparer.OrdinalIgnoreCase);

                    IReadOnlyList<BestiaryClassResponse> classes = BestiaryCatalog.Classes
                                                                                  .OrderBy(entry => entry.SortOrder)
                                                                                  .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                                                                                  .Select(entry => MapBestiaryClass(
                                                                                      entry,
                                                                                      creatureCounts.GetValueOrDefault(entry.Slug, 0)))
                                                                                  .ToList();
                    return classes;
                },
                _cacheOptions,
                [CacheTags.Bestiary],
                cancellationToken);
        }

        public async Task<IReadOnlyList<BestiaryCategoryResponse>> GetBestiaryCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "bestiary:categories",
                async ct =>
                {
                    IReadOnlyList<BestiaryCreatureSource> sources = await LoadBestiaryCreatureSourcesAsync(ct);
                    Dictionary<string, int> creatureCounts = sources.GroupBy(
                                                                        entry => NormalizeBestiarySlug(entry.CategorySlug) ?? entry.ClassSlug,
                                                                        StringComparer.OrdinalIgnoreCase)
                                                                    .ToDictionary(
                                                                        group => group.Key,
                                                                        group => group.Count(),
                                                                        StringComparer.OrdinalIgnoreCase);

                    return BestiaryCatalog.Categories
                                          .OrderBy(entry => entry.SortOrder)
                                          .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                                          .Select(entry => MapBestiaryCategory(
                                              entry,
                                              creatureCounts.GetValueOrDefault(entry.Slug, 0)))
                                          .ToList();
                },
                _cacheOptions,
                [CacheTags.Bestiary],
                cancellationToken);
        }

        public async Task<IReadOnlyList<BestiaryDifficultyResponse>> GetBestiaryDifficultiesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "bestiary:difficulties",
                async ct =>
                {
                    return BestiaryCatalog.Difficulties
                                          .OrderBy(entry => entry.SortOrder)
                                          .Select(entry => MapBestiaryDifficulty(entry, BestiaryOccurrence.Ordinary))
                                          .ToList();
                },
                _cacheOptions,
                [CacheTags.Bestiary],
                cancellationToken);
        }
        
        public async Task<BestiaryCategoryCreaturesResponse?> GetBestiaryCreaturesByCategoryAsync(string categorySlug, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(categorySlug))
            {
                return null;
            }

            string? normalizedSlug = NormalizeBestiarySlug(categorySlug);

            if(string.IsNullOrWhiteSpace(normalizedSlug))
            {
                return null;
            }

            BestiaryClassDefinition? categoryDefinition = BestiaryCatalog.Categories.FirstOrDefault(entry =>
                string.Equals(entry.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));

            if(categoryDefinition is null)
            {
                return null;
            }

            string cacheKey = $"bestiary:category-creatures:{normalizedSlug}".ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IReadOnlyList<BestiaryCreatureSource> sources = await LoadBestiaryCreatureSourcesAsync(ct);

                    List<BestiaryCreatureSource> filteredSources = sources
                                                                   .Where(entry => string.Equals(
                                                                       NormalizeBestiarySlug(entry.CategorySlug) ?? entry.ClassSlug,
                                                                       normalizedSlug,
                                                                       StringComparison.OrdinalIgnoreCase))
                                                                   .OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
                                                                   .ToList();

                    return MapBestiaryCategoryCreatures(categoryDefinition, filteredSources);
                },
                _cacheOptions,
                [CacheTags.Bestiary],
                cancellationToken);
        }

        public async Task<BestiaryDifficultyCreaturesResponse?> GetBestiaryCreaturesByDifficultyAsync(string difficultySlug, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(difficultySlug))
            {
                return null;
            }

            string? normalizedSlug = NormalizeBestiarySlug(difficultySlug);

            if(string.IsNullOrWhiteSpace(normalizedSlug))
            {
                return null;
            }

            BestiaryDifficultyDefinition? difficultyDefinition = BestiaryCatalog.Difficulties.FirstOrDefault(entry =>
                string.Equals(entry.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));

            if(difficultyDefinition is null)
            {
                return null;
            }

            string cacheKey = $"bestiary:difficulty-creatures:{normalizedSlug}".ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IReadOnlyList<BestiaryCreatureSource> sources = await LoadBestiaryCreatureSourcesAsync(ct);

                    List<BestiaryCreatureSource> filteredSources = sources
                                                                   .Where(entry => string.Equals(
                                                                       entry.DifficultySlug,
                                                                       normalizedSlug,
                                                                       StringComparison.OrdinalIgnoreCase))
                                                                   .OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
                                                                   .ToList();

                    return MapBestiaryDifficultyCreatures(
                        difficultyDefinition,
                        BestiaryOccurrence.Ordinary,
                        filteredSources);
                },
                _cacheOptions,
                [CacheTags.Bestiary],
                cancellationToken);
        }

        public async Task<IReadOnlyList<BestiaryCharmPointOverviewItemResponse>> GetBestiaryCharmPointOverviewAsync(string? sortBy = null, bool descending = false,
            CancellationToken cancellationToken = default)
        {
            string normalizedSortBy = NormalizeSortField(sortBy, "points");
            string cacheKey = $"bestiary:charm-points:{normalizedSortBy}:{descending}".ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IReadOnlyList<BestiaryCreatureSource> sources = await LoadBestiaryCreatureSourcesAsync(ct);

                    List<BestiaryCharmPointOverviewItemResponse> items = sources.Select(MapBestiaryCharmPointOverviewItem)
                                                                                 .ToList();

                    return SortCharmPointOverview(items, normalizedSortBy, descending);
                },
                _cacheOptions,
                [CacheTags.Bestiary],
                cancellationToken);
        }

        public async Task<BestiaryFilteredCreaturesResponse> GetFilteredBestiaryCreaturesAsync(string? classSlug = null, string? categorySlug = null, string? difficultySlug = null, int? charmPoints = null,
            string? search = null, string? sortBy = null,
            bool descending = false, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
        {
            string? normalizedClassSlug = NormalizeBestiarySlug(classSlug);
            string? normalizedCategorySlug = NormalizeBestiarySlug(categorySlug);
            string? normalizedDifficultySlug = NormalizeBestiarySlug(difficultySlug);
            string normalizedSortBy = NormalizeSortField(sortBy, "name");
            int sanitizedPage = Math.Max(1, page);
            int sanitizedPageSize = Math.Clamp(pageSize, 1, 250);
            string? normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            string cacheKey =
                $"bestiary:filtered:{normalizedClassSlug}:{normalizedCategorySlug}:{normalizedDifficultySlug}:{charmPoints}:{normalizedSearch}:{normalizedSortBy}:{descending}:{sanitizedPage}:{sanitizedPageSize}"
                .ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IReadOnlyList<BestiaryCreatureSource> allSources = await LoadBestiaryCreatureSourcesAsync(ct);

                    IEnumerable<BestiaryCreatureSource> query = allSources;

                    if(!string.IsNullOrWhiteSpace(normalizedClassSlug))
                    {
                        query = query.Where(entry => string.Equals(entry.ClassSlug, normalizedClassSlug, StringComparison.OrdinalIgnoreCase));
                    }

                    if(!string.IsNullOrWhiteSpace(normalizedCategorySlug))
                    {
                        query = query.Where(entry =>
                            string.Equals(
                                NormalizeBestiarySlug(entry.CategorySlug) ?? entry.ClassSlug,
                                normalizedCategorySlug,
                                StringComparison.OrdinalIgnoreCase));
                    }

                    if(!string.IsNullOrWhiteSpace(normalizedDifficultySlug))
                    {
                        query = query.Where(entry => string.Equals(entry.DifficultySlug, normalizedDifficultySlug, StringComparison.OrdinalIgnoreCase));
                    }

                    if(charmPoints is > 0)
                    {
                        query = query.Where(entry =>
                        {
                            BestiaryDifficultyDefinition definition = GetRequiredDifficultyDefinition(entry.DifficultySlug);
                            return GetCharmPoints(entry, definition) == charmPoints.Value;
                        });
                    }

                    if(!string.IsNullOrWhiteSpace(normalizedSearch))
                    {
                        query = query.Where(entry =>
                            entry.CreatureName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
                    }

                    List<BestiaryCreatureSource> filteredSources = query.ToList();
                    int totalCount = filteredSources.Count;

                    filteredSources = SortCreatureSources(filteredSources, normalizedSortBy, descending)
                                     .Skip((sanitizedPage - 1) * sanitizedPageSize)
                                     .Take(sanitizedPageSize)
                                     .ToList();

                    return MapFilteredCreatures(
                        normalizedClassSlug,
                        normalizedCategorySlug,
                        normalizedDifficultySlug,
                        charmPoints,
                        normalizedSearch,
                        normalizedSortBy,
                        descending,
                        sanitizedPage,
                        sanitizedPageSize,
                        totalCount,
                        filteredSources);
                },
                _cacheOptions,
                [CacheTags.Bestiary],
                cancellationToken);
        }

        private async Task<IReadOnlyList<BestiaryCreatureSource>> LoadBestiaryCreatureSourcesAsync(CancellationToken cancellationToken)
        {
            List<BestiaryCreatureRecord> records = await db.Creatures
                                                           .AsNoTracking()
                                                           .Where(entry => entry.BestiaryJson != null && entry.BestiaryJson != string.Empty)
                                                           .OrderBy(entry => entry.Name)
                                                           .Select(entry => new BestiaryCreatureRecord(
                                                               entry.Id,
                                                               entry.Name,
                                                               entry.LastUpdated,
                                                               entry.BestiaryJson))
                                                           .ToListAsync(cancellationToken);

            return records.Select(entry => ParseBestiaryCreatureSource(
                                                  entry.Id,
                                                  entry.Name,
                                                  entry.LastUpdated,
                                                  entry.BestiaryJson))
                          .Where(entry => entry is not null)
                          .Select(entry => entry!)
                          .OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
                          .ToList();
        }

        private static BestiaryClassResponse MapBestiaryClass(
            BestiaryClassDefinition definition,
            int creatureCount)
        {
            return new BestiaryClassResponse(
                definition.SortOrder,
                definition.Name,
                definition.Slug,
                definition.SortOrder,
                1,
                creatureCount);
        }

        private static BestiaryCategoryResponse MapBestiaryCategory(
            BestiaryClassDefinition definition,
            int creatureCount)
        {
            return new BestiaryCategoryResponse(
                definition.SortOrder,
                definition.Name,
                definition.Slug,
                definition.Name,
                definition.Slug,
                creatureCount);
        }

        private static BestiaryDifficultyResponse MapBestiaryDifficulty(
            BestiaryDifficultyDefinition definition,
            BestiaryOccurrence occurrence)
        {
            IReadOnlyList<BestiaryLevelRequirementResponse> levelRequirements =
            MapLevelRequirements(definition.GetLevelRequirements(occurrence));

            return new BestiaryDifficultyResponse(
                definition.Name,
                definition.Slug,
                definition.SortOrder,
                definition.GetCharmPoints(occurrence),
                levelRequirements.Sum(entry => entry.KillsRequired),
                levelRequirements);
        }

        private static BestiaryCreatureListItemResponse MapBestiaryCreatureListItem(BestiaryCreatureSource source)
        {
            BestiaryClassDefinition classDefinition = GetRequiredClassDefinition(source.ClassSlug);
            BestiaryClassDefinition categoryDefinition = GetRequiredCategoryDefinition(source.CategorySlug, classDefinition);
            BestiaryDifficultyDefinition difficultyDefinition = GetRequiredDifficultyDefinition(source.DifficultySlug);
            IReadOnlyList<BestiaryLevelRequirementResponse> levelRequirements = GetLevelRequirements(source, difficultyDefinition);
            int totalKillsRequired = GetTotalKillsRequired(source, difficultyDefinition, levelRequirements);

            return new BestiaryCreatureListItemResponse(
                source.CreatureId,
                source.CreatureName,
                classDefinition.Name,
                classDefinition.Slug,
                categoryDefinition.Name,
                categoryDefinition.Slug,
                difficultyDefinition.Name,
                difficultyDefinition.Slug,
                difficultyDefinition.SortOrder,
                GetCharmPoints(source, difficultyDefinition),
                totalKillsRequired,
                levelRequirements,
                source.LastUpdated);
        }

        private static BestiaryCharmPointOverviewItemResponse MapBestiaryCharmPointOverviewItem(BestiaryCreatureSource source)
        {
            BestiaryClassDefinition classDefinition = GetRequiredClassDefinition(source.ClassSlug);
            BestiaryClassDefinition categoryDefinition = GetRequiredCategoryDefinition(source.CategorySlug, classDefinition);
            BestiaryDifficultyDefinition difficultyDefinition = GetRequiredDifficultyDefinition(source.DifficultySlug);
            IReadOnlyList<BestiaryLevelRequirementResponse> levelRequirements = GetLevelRequirements(source, difficultyDefinition);

            return new BestiaryCharmPointOverviewItemResponse(
                source.CreatureId,
                source.CreatureName,
                classDefinition.Name,
                categoryDefinition.Name,
                difficultyDefinition.Name,
                difficultyDefinition.SortOrder,
                GetCharmPoints(source, difficultyDefinition),
                GetTotalKillsRequired(source, difficultyDefinition, levelRequirements),
                source.LastUpdated);
        }

        private static BestiaryCategoryCreaturesResponse MapBestiaryCategoryCreatures(
            BestiaryClassDefinition categoryDefinition,
            IReadOnlyList<BestiaryCreatureSource> sources)
        {
            List<BestiaryCreatureListItemResponse> creatures = sources.Select(MapBestiaryCreatureListItem)
                                                                      .OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
                                                                      .ToList();

            return new BestiaryCategoryCreaturesResponse(
                categoryDefinition.Name,
                categoryDefinition.Slug,
                categoryDefinition.Name,
                categoryDefinition.Slug,
                creatures.Count,
                creatures);
        }

        private static BestiaryDifficultyCreaturesResponse MapBestiaryDifficultyCreatures(
            BestiaryDifficultyDefinition difficultyDefinition,
            BestiaryOccurrence occurrence,
            IReadOnlyList<BestiaryCreatureSource> sources)
        {
            IReadOnlyList<BestiaryLevelRequirementResponse> levelRequirements =
            MapLevelRequirements(difficultyDefinition.GetLevelRequirements(occurrence));

            List<BestiaryCreatureListItemResponse> creatures = sources.Select(MapBestiaryCreatureListItem)
                                                                      .OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
                                                                      .ToList();

            return new BestiaryDifficultyCreaturesResponse(
                difficultyDefinition.Name,
                difficultyDefinition.Slug,
                difficultyDefinition.SortOrder,
                difficultyDefinition.GetCharmPoints(occurrence),
                levelRequirements.Sum(entry => entry.KillsRequired),
                levelRequirements,
                creatures.Count,
                creatures);
        }

        private static BestiaryFilteredCreaturesResponse MapFilteredCreatures(
            string? classSlug,
            string? categorySlug,
            string? difficultySlug,
            int? charmPoints,
            string? search,
            string? sortBy,
            bool descending,
            int page,
            int pageSize,
            int totalCount,
            IReadOnlyList<BestiaryCreatureSource> sources)
        {
            return new BestiaryFilteredCreaturesResponse(
                NormalizeBestiarySlug(classSlug),
                NormalizeBestiarySlug(categorySlug),
                NormalizeBestiarySlug(difficultySlug),
                charmPoints,
                string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                MapPublicSortField(sortBy),
                descending,
                page,
                pageSize,
                totalCount,
                sources.Select(MapBestiaryCreatureListItem).ToList());
        }

        private static List<BestiaryCharmPointOverviewItemResponse> SortCharmPointOverview(
            List<BestiaryCharmPointOverviewItemResponse> items,
            string sortBy,
            bool descending)
        {
            IOrderedEnumerable<BestiaryCharmPointOverviewItemResponse> ordered = sortBy switch
            {
                "name" => items.OrderBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "class" => items.OrderBy(entry => entry.ClassName, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "category" => items.OrderBy(entry => entry.CategoryName, StringComparer.OrdinalIgnoreCase)
                                   .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "difficulty" => items.OrderBy(entry => entry.DifficultySortOrder)
                                     .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "updated" => items.OrderBy(entry => entry.LastUpdated)
                                  .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "kills" => items.OrderBy(entry => entry.TotalKillsRequired)
                                .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                _ => items.OrderBy(entry => entry.CharmPoints)
                          .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase)
            };

            return descending ? ordered.Reverse().ToList() : ordered.ToList();
        }

        private static List<BestiaryCreatureSource> SortCreatureSources(
            List<BestiaryCreatureSource> sources,
            string sortBy,
            bool descending)
        {
            IOrderedEnumerable<BestiaryCreatureSource> ordered = sortBy switch
            {
                "class" => sources.OrderBy(entry => GetRequiredClassDefinition(entry.ClassSlug).SortOrder)
                                  .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "category" => sources.OrderBy(entry => GetRequiredCategoryDefinition(
                                          entry.CategorySlug,
                                          GetRequiredClassDefinition(entry.ClassSlug)).SortOrder)
                                     .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "difficulty" => sources.OrderBy(entry => GetRequiredDifficultyDefinition(entry.DifficultySlug).SortOrder)
                                       .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "charm-points" => sources.OrderBy(entry => GetCharmPoints(entry, GetRequiredDifficultyDefinition(entry.DifficultySlug)))
                                         .ThenBy(entry => entry.CreatureName, StringComparer.OrdinalIgnoreCase),
                "kills" => sources.OrderBy(entry =>
                                   {
                                       BestiaryDifficultyDefinition definition = GetRequiredDifficultyDefinition(entry.DifficultySlug);
                                       IReadOnlyList<BestiaryLevelRequirementResponse> levels = GetLevelRequirements(entry, definition);
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
            string normalized = NormalizeBestiarySlug(sortBy) ?? string.Empty;

            return normalized switch
            {
                "name" => "name",
                "class" => "class",
                "category" => "category",
                "difficulty" => "difficulty",
                "points" => "charm-points",
                "charmpoints" => "charm-points",
                "charm-points" => "charm-points",
                "totalkillsrequired" => "kills",
                "total-kills-required" => "kills",
                "kills" => "kills",
                "lastupdated" => "updated",
                "last-updated" => "updated",
                "updated" => "updated",
                _ => fallback
            };
        }

        private static string? MapPublicSortField(string? sortBy)
        {
            string normalized = NormalizeSortField(sortBy, "name");

            return normalized switch
            {
                "class" => "bestiary-class",
                "kills" => "total-kills",
                "updated" => "last-updated",
                _ => normalized
            };
        }

        private static BestiaryCreatureSource? ParseBestiaryCreatureSource(
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

                string? classSlug = GetJsonString(values, "classSlug") ??
                                    GetJsonString(values, "class") ??
                                    GetJsonString(values, "className");

                string? difficultySlug = GetJsonString(values, "difficultySlug") ??
                                         GetJsonString(values, "difficulty") ??
                                         GetJsonString(values, "difficultyName");

                if(string.IsNullOrWhiteSpace(classSlug) || string.IsNullOrWhiteSpace(difficultySlug))
                {
                    return null;
                }

                List<BestiaryLevelRequirementResponse>? levelRequirements = ParseLevelRequirements(values);
                string? normalizedClassSlug = NormalizeBestiarySlug(classSlug);
                string? normalizedCategorySlug = NormalizeBestiarySlug(
                    GetJsonString(values, "categorySlug") ??
                    GetJsonString(values, "category") ??
                    GetJsonString(values, "categoryName") ??
                    classSlug);
                string? normalizedDifficultySlug = NormalizeBestiarySlug(difficultySlug);

                if(string.IsNullOrWhiteSpace(normalizedClassSlug) ||
                   string.IsNullOrWhiteSpace(normalizedDifficultySlug) ||
                   !IsKnownBestiaryClass(normalizedClassSlug) ||
                   !IsKnownBestiaryDifficulty(normalizedDifficultySlug))
                {
                    return null;
                }

                if(string.IsNullOrWhiteSpace(normalizedCategorySlug) || !IsKnownBestiaryClass(normalizedCategorySlug))
                {
                    normalizedCategorySlug = normalizedClassSlug;
                }

                return new BestiaryCreatureSource(
                    creatureId,
                    creatureName,
                    normalizedClassSlug,
                    normalizedCategorySlug,
                    normalizedDifficultySlug,
                    ParseOccurrence(
                        GetJsonString(values, "occurrence") ??
                        GetJsonString(values, "rarity")),
                    GetJsonInt32(values, "charmPoints"),
                    GetJsonInt32(values, "totalKillsRequired"),
                    levelRequirements,
                    lastUpdated);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static BestiaryClassDefinition GetRequiredClassDefinition(string classSlug)
        {
            return BestiaryCatalog.GetRequiredClass(NormalizeBestiarySlug(classSlug)!);
        }

        private static bool IsKnownBestiaryClass(string slug)
        {
            return BestiaryCatalog.Classes.Any(entry =>
                string.Equals(entry.Slug, slug, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsKnownBestiaryDifficulty(string slug)
        {
            return BestiaryCatalog.Difficulties.Any(entry =>
                string.Equals(entry.Slug, slug, StringComparison.OrdinalIgnoreCase));
        }

        private static BestiaryClassDefinition GetRequiredCategoryDefinition(
            string? categorySlug,
            BestiaryClassDefinition classDefinition)
        {
            string normalizedCategorySlug = NormalizeBestiarySlug(categorySlug) ?? classDefinition.Slug;
            return BestiaryCatalog.GetRequiredCategory(normalizedCategorySlug);
        }

        private static BestiaryDifficultyDefinition GetRequiredDifficultyDefinition(string difficultySlug)
        {
            return BestiaryCatalog.GetRequiredDifficulty(NormalizeBestiarySlug(difficultySlug)!);
        }

        private static int GetCharmPoints(
            BestiaryCreatureSource source,
            BestiaryDifficultyDefinition difficultyDefinition)
        {
            return source.CharmPoints is > 0
            ? source.CharmPoints.Value
            : difficultyDefinition.GetCharmPoints(source.Occurrence);
        }

        private static int GetTotalKillsRequired(
            BestiaryCreatureSource source,
            BestiaryDifficultyDefinition difficultyDefinition,
            IReadOnlyList<BestiaryLevelRequirementResponse> levelRequirements)
        {
            return source.TotalKillsRequired is > 0
            ? source.TotalKillsRequired.Value
            : levelRequirements.Count > 0
            ? levelRequirements.Sum(entry => entry.KillsRequired)
            : difficultyDefinition.GetTotalKillsRequired(source.Occurrence);
        }

        private static IReadOnlyList<BestiaryLevelRequirementResponse> GetLevelRequirements(
            BestiaryCreatureSource source,
            BestiaryDifficultyDefinition difficultyDefinition)
        {
            return source.LevelRequirements is { Count: > 0 }
            ? source.LevelRequirements
            : MapLevelRequirements(difficultyDefinition.GetLevelRequirements(source.Occurrence));
        }

        private static IReadOnlyList<BestiaryLevelRequirementResponse> MapLevelRequirements(
            IReadOnlyList<BestiaryLevelDefinition> levelRequirements)
        {
            return levelRequirements.Select(entry => new BestiaryLevelRequirementResponse(
                                        entry.Level,
                                        entry.Name,
                                        entry.KillsRequired))
                                    .ToList();
        }

        private static List<BestiaryLevelRequirementResponse>? ParseLevelRequirements(
            IReadOnlyDictionary<string, JsonElement> values)
        {
            string[] levelRequirementKeys = ["levelRequirements", "levels", "killStages"];

            foreach(string key in levelRequirementKeys)
            {
                if(!values.TryGetValue(key, out JsonElement element) || element.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                List<BestiaryLevelRequirementResponse> entries = [];

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

                    if(killsRequired <= 0)
                    {
                        continue;
                    }

                    entries.Add(new BestiaryLevelRequirementResponse(level, levelName, killsRequired));
                }

                return entries.Count == 0 ? null : entries;
            }

            return null;
        }

        private static BestiaryOccurrence ParseOccurrence(string? value)
        {
            string normalized = NormalizeBestiarySlug(value) ?? string.Empty;

            return normalized switch
            {
                "very-rare" => BestiaryOccurrence.VeryRare,
                "veryrare" => BestiaryOccurrence.VeryRare,
                "rare" => BestiaryOccurrence.VeryRare,
                _ => BestiaryOccurrence.Ordinary
            };
        }

        private static string? NormalizeBestiarySlug(string? value)
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

        private sealed record BestiaryCreatureSource(
            int CreatureId,
            string CreatureName,
            string ClassSlug,
            string? CategorySlug,
            string DifficultySlug,
            BestiaryOccurrence Occurrence,
            int? CharmPoints,
            int? TotalKillsRequired,
            IReadOnlyList<BestiaryLevelRequirementResponse>? LevelRequirements,
            DateTime LastUpdated);

        private sealed record BestiaryCreatureRecord(
            int Id,
            string Name,
            DateTime LastUpdated,
            string? BestiaryJson);
    }
}
