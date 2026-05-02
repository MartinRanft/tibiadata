using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Creatures;
using TibiaDataApi.Contracts.Public.LootStatistics;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.DataBaseService.Creatures.Interfaces;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Creatures
{
    public sealed class CreaturesDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : ICreaturesDataBaseService
    {
        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<List<string>> GetCreaturesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "creatures:names",
                async ct =>
                {
                    return await db.Creatures
                                   .AsNoTracking()
                                   .OrderBy(x => x.Name)
                                   .Select(x => x.Name)
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Creatures],
                cancellationToken);
        }

        public async Task<PagedResponse<CreatureListItemResponse>> GetCreatureListAsync(
            int page,
            int pageSize,
            string? creatureName = null,
            int? minHitpoints = null,
            int? maxHitpoints = null,
            long? minExperience = null,
            long? maxExperience = null,
            string? sort = null,
            bool descending = false,
            CancellationToken cancellationToken = default)
        {
            int normalizedPage = page < 1 ? 1 : page;
            int normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            string normalizedCreatureName = EntityNameNormalizer.NormalizeOptional(creatureName) ?? string.Empty;
            string normalizedSort = NormalizeCreatureSort(sort);
            string cacheKey = $"creatures:list:{normalizedPage}:{normalizedPageSize}:{normalizedCreatureName}:{minHitpoints}:{maxHitpoints}:{minExperience}:{maxExperience}:{normalizedSort}:{descending}"
                .ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    IQueryable<Creature> query = db.Creatures
                                                   .AsNoTracking();

                    if(!string.IsNullOrWhiteSpace(normalizedCreatureName))
                    {
                        query = query.Where(x => x.NormalizedName.Contains(normalizedCreatureName));
                    }

                    if(minHitpoints.HasValue)
                    {
                        query = query.Where(x => x.Hitpoints >= minHitpoints.Value);
                    }

                    if(maxHitpoints.HasValue)
                    {
                        query = query.Where(x => x.Hitpoints <= maxHitpoints.Value);
                    }

                    if(minExperience.HasValue)
                    {
                        query = query.Where(x => x.Experience >= minExperience.Value);
                    }

                    if(maxExperience.HasValue)
                    {
                        query = query.Where(x => x.Experience <= maxExperience.Value);
                    }

                    int totalCount = await query.CountAsync(ct);
                    IQueryable<Creature> pagedQuery = ApplyCreatureSorting(query, normalizedSort, descending)
                                                      .Skip((normalizedPage - 1) * normalizedPageSize)
                                                      .Take(normalizedPageSize);

                    List<CreatureListItemResponse> creatures = await ProjectCreatureListItems(pagedQuery)
                    .ToListAsync(ct);

                    return new PagedResponse<CreatureListItemResponse>(normalizedPage, normalizedPageSize, totalCount, creatures);
                },
                _cacheOptions,
                [CacheTags.Creatures],
                cancellationToken);
        }

        public async Task<CreatureDetailsResponse?> GetCreatureDetailsByNameAsync(string creatureName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(creatureName);

            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            CachedCreatureDetailsResult detailsResult = await hybridCache.GetOrCreateAsync(
                $"creatures:by-name:{normalizedName}",
                async ct =>
                {
                    int exactId = await db.Creatures
                                          .AsNoTracking()
                                          .Where(x => x.NormalizedName == normalizedName)
                                          .Select(x => x.Id)
                                          .SingleOrDefaultAsync(ct);

                    if(exactId > 0)
                    {
                        return new CachedCreatureDetailsResult(true, await GetCreatureDetailsByIdAsync(exactId, ct)!);
                    }

                    List<int> actualNameMatches = db.Creatures
                                                    .AsNoTracking()
                                                    .Where(x => x.NormalizedName == normalizedName)
                                                    .OrderBy(x => x.Name)
                                                    .Select(x => x.Id)
                                                    .Take(2)
                                                    .ToList();

                    if(actualNameMatches.Count != 1)
                    {
                        return CachedCreatureDetailsResult.NotFound;
                    }

                    CreatureDetailsResponse? creatureByActualName = await GetCreatureDetailsByIdAsync(actualNameMatches[0], ct)!;
                    return new CachedCreatureDetailsResult(true, creatureByActualName);
                },
                _cacheOptions,
                [CacheTags.Creatures],
                cancellationToken);

            return detailsResult.Found ? detailsResult.Creature : null;
        }

        public async Task<CreatureDetailsResponse?> GetCreatureDetailsByIdAsync(int creatureId, CancellationToken cancellationToken = default)
        {
            if(creatureId == 0)
            {
                return null;
            }

            CachedCreatureDetailsResult result = await hybridCache.GetOrCreateAsync(
                $"creatures:by-id:{creatureId}",
                async ct =>
                {
                    Creature? creature = await db.Creatures
                                                 .AsNoTracking()
                                                 .Include(x => x.CreatureAssets)
                                                 .ThenInclude(x => x.Asset)
                                                 .Where(x => x.Id == creatureId)
                                                 .SingleOrDefaultAsync(ct);

                    if(creature is null)
                    {
                        return CachedCreatureDetailsResult.NotFound;
                    }

                    List<CreatureImageResponse> images = creature.CreatureAssets
                                                                 .Where(x => x.Asset != null)
                                                                 .OrderBy(x => x.SortOrder)
                                                                 .Select(x => new CreatureImageResponse(
                                                                     x.AssetId,
                                                                     x.Asset!.StorageKey,
                                                                     x.Asset.FileName,
                                                                     x.Asset.MimeType,
                                                                     x.Asset.Width,
                                                                     x.Asset.Height))
                                                                 .ToList();

                    CreatureDetailsResponse response = new(
                        creature.Id,
                        creature.Name,
                        creature.Hitpoints,
                        creature.Experience,
                        CreateStructuredData(creature),
                        ParseLootStatistics(creature.LootStatisticsJson),
                        images,
                        creature.LastUpdated);

                    return new CachedCreatureDetailsResult(true, response);
                },
                _cacheOptions,
                [CacheTags.Creatures],
                cancellationToken);

            return result.Found ? result.Creature : null;
        }

        public async Task<LootStatisticDetailsResponse?> GetCreatureLootByNameAsync(string creatureName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(creatureName);

            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            CachedCreatureLootResult result = await hybridCache.GetOrCreateAsync(
                $"creatures:loot:by-name:{normalizedName}",
                async ct =>
                {
                    int exactId = await db.Creatures
                                          .AsNoTracking()
                                          .Where(x => x.NormalizedName == normalizedName)
                                          .Select(x => x.Id)
                                          .SingleOrDefaultAsync(ct);

                    if(exactId <= 0)
                    {
                        return CachedCreatureLootResult.NotFound;
                    }

                    LootStatisticDetailsResponse? loot = await GetCreatureLootByIdAsync(exactId, ct);
                    return loot is null
                    ? CachedCreatureLootResult.NotFound
                    : new CachedCreatureLootResult(true, loot);
                },
                _cacheOptions,
                [CacheTags.Creatures],
                cancellationToken);

            return result.Found ? result.Loot : null;
        }

        public async Task<LootStatisticDetailsResponse?> GetCreatureLootByIdAsync(int creatureId, CancellationToken cancellationToken = default)
        {
            if(creatureId <= 0)
            {
                return null;
            }

            CachedCreatureLootResult result = await hybridCache.GetOrCreateAsync(
                $"creatures:loot:by-id:{creatureId}",
                async ct =>
                {
                    Creature? creature = await db.Creatures
                                                 .AsNoTracking()
                                                 .Where(x => x.Id == creatureId)
                                                 .SingleOrDefaultAsync(ct);

                    if(creature is null)
                    {
                        return CachedCreatureLootResult.NotFound;
                    }

                    return new CachedCreatureLootResult(true, MapCreatureLoot(creature));
                },
                _cacheOptions,
                [CacheTags.Creatures],
                cancellationToken);

            return result.Found ? result.Loot : null;
        }

        public async Task<List<SyncStateResponse>> GetCreatureSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "creatures:sync-states",
                async ct =>
                {
                    return await db.Creatures
                                   .AsNoTracking()
                                   .OrderBy(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Creatures],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetCreatureSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "creatures:sync-states-bydatetime",
                async ct =>
                {
                    return await db.Creatures
                                   .AsNoTracking()
                                   .Where(x => x.LastUpdated >= time)
                                   .OrderBy(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Creatures],
                cancellationToken);
        }

        private static IReadOnlyList<LootStatisticEntryResponse> ParseLootStatistics(string? lootStatisticsJson)
        {
            if(string.IsNullOrWhiteSpace(lootStatisticsJson))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<LootStatisticEntryResponse>>(lootStatisticsJson)
                       ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static CreatureStructuredDataResponse? CreateStructuredData(Creature creature)
        {
            IReadOnlyDictionary<string, string>? infobox = StructuredJsonParser.ParseStringDictionary(creature.InfoboxJson);

            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            IReadOnlyDictionary<string, string>? bestiary = StructuredJsonParser.ParseStringDictionary(creature.BestiaryJson);

            CreatureInfoboxResponse creatureInfobox = new(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "actualname"),
                GetInfoboxValue(infobox, "plural"),
                GetInfoboxValue(infobox, "article"),
                GetInfoboxValue(infobox, "armor"),
                GetInfoboxValue(infobox, "mitigation"),
                GetInfoboxValue(infobox, "summon"),
                GetInfoboxValue(infobox, "convince"),
                GetInfoboxValue(infobox, "illusionable"),
                GetInfoboxValue(infobox, "isboss"),
                GetInfoboxValue(infobox, "creatureclass"),
                GetInfoboxValue(infobox, "primarytype"),
                GetInfoboxValue(infobox, "secondarytype"),
                GetInfoboxValue(infobox, "abilities"),
                GetInfoboxValue(infobox, "maxdmg"),
                GetInfoboxValue(infobox, "pushable"),
                GetInfoboxValue(infobox, "pushobjects"),
                GetInfoboxValue(infobox, "walksaround"),
                GetInfoboxValue(infobox, "walksthrough"),
                GetInfoboxValue(infobox, "physicaldmgmod"),
                GetInfoboxValue(infobox, "earthdmgmod"),
                GetInfoboxValue(infobox, "firedmgmod"),
                GetInfoboxValue(infobox, "deathdmgmod"),
                GetInfoboxValue(infobox, "energydmgmod"),
                GetInfoboxValue(infobox, "holydmgmod"),
                GetInfoboxValue(infobox, "icedmgmod"),
                GetInfoboxValue(infobox, "hpdraindmgmod"),
                GetInfoboxValue(infobox, "drowndmgmod"),
                GetInfoboxValue(infobox, "healmod"),
                GetInfoboxValue(infobox, "sounds"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "raceid", "race_id"),
                GetInfoboxValue(infobox, "notes"),
                GetInfoboxValue(infobox, "behaviour"),
                GetInfoboxValue(infobox, "runsat"),
                GetInfoboxValue(infobox, "speed"),
                GetInfoboxValue(infobox, "strategy"),
                GetInfoboxValue(infobox, "location"),
                GetInfoboxValue(infobox, "history"),
                GetInfoboxValue(infobox, "usespells"),
                GetInfoboxValue(infobox, "attacktype"),
                GetInfoboxValue(infobox, "spawntype"),
                GetInfoboxValue(bestiary, "classslug"),
                GetInfoboxValue(bestiary, "difficultyslug"),
                GetInfoboxValue(bestiary, "occurrence"),
                GetInfoboxValue(bestiary, "bosstiarycategory"),
                infobox);

            CreatureResistanceSummaryResponse? resistanceSummary = CreateResistanceSummary(infobox);
            CreatureCombatPropertiesResponse? combatProperties = CreateCombatProperties(infobox);

            return new CreatureStructuredDataResponse(
                "Infobox Creature",
                creatureInfobox,
                resistanceSummary,
                combatProperties);
        }

        private static LootStatisticDetailsResponse MapCreatureLoot(Creature creature)
        {
            return new LootStatisticDetailsResponse(
                creature.Id,
                creature.Name,
                ParseLootStatistics(creature.LootStatisticsJson),
                creature.LastUpdated);
        }

        private static CreatureResistanceSummaryResponse? CreateResistanceSummary(IReadOnlyDictionary<string, string> infobox)
        {
            CreatureResistanceSummaryResponse response = new(
                ParseNormalizedNumber(GetInfoboxValue(infobox, "physicaldmgmod")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "earthdmgmod")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "firedmgmod")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "deathdmgmod")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "energydmgmod")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "holydmgmod")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "icedmgmod")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "hpdraindmgmod")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "drowndmgmod")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "healmod")));

            return response is
            {
                PhysicalPercent: null,
                EarthPercent: null,
                FirePercent: null,
                DeathPercent: null,
                EnergyPercent: null,
                HolyPercent: null,
                IcePercent: null,
                LifeDrainPercent: null,
                DrownPercent: null,
                HealingPercent: null
            }
            ? null
            : response;
        }

        private static CreatureCombatPropertiesResponse? CreateCombatProperties(IReadOnlyDictionary<string, string> infobox)
        {
            CreatureCombatPropertiesResponse response = new(
                ParseNormalizedNumber(GetInfoboxValue(infobox, "armor")),
                ParseNormalizedDecimal(GetInfoboxValue(infobox, "mitigation")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "maxdmg")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "speed")),
                ParseNormalizedNumber(GetInfoboxValue(infobox, "runsat")),
                ParseNormalizedBoolean(GetInfoboxValue(infobox, "isboss")),
                ParseNormalizedBoolean(GetInfoboxValue(infobox, "usespells")),
                ParseNormalizedBoolean(GetInfoboxValue(infobox, "pushable")),
                ParseNormalizedBoolean(GetInfoboxValue(infobox, "pushobjects")),
                ParseNormalizedBoolean(GetInfoboxValue(infobox, "walksaround")));

            return response is
            {
                Armor: null,
                Mitigation: null,
                MaxDamage: null,
                Speed: null,
                RunsAt: null,
                IsBoss: null,
                UsesSpells: null,
                Pushable: null,
                PushObjects: null,
                WalksAround: null
            }
            ? null
            : response;
        }

        private static IQueryable<CreatureListItemResponse> ProjectCreatureListItems(IQueryable<Creature> query)
        {
            return query.Select(x => new CreatureListItemResponse(
                x.Id,
                x.Name,
                x.Hitpoints,
                x.Experience,
                x.CreatureAssets
                 .OrderBy(asset => asset.SortOrder)
                 .Select(asset => asset.Asset == null
                 ? null
                 : new CreatureImageResponse(
                     asset.AssetId,
                     asset.Asset.StorageKey,
                     asset.Asset.FileName,
                     asset.Asset.MimeType,
                     asset.Asset.Width,
                     asset.Asset.Height))
                 .FirstOrDefault(),
                x.LastUpdated));
        }

        private static string NormalizeCreatureSort(string? sort)
        {
            string normalized = string.IsNullOrWhiteSpace(sort)
            ? string.Empty
            : sort.Trim().ToLowerInvariant();

            return normalized switch
            {
                "" => "name",
                "name" => "name",
                "hitpoints" => "hitpoints",
                "experience" => "experience",
                "last-updated" => "last-updated",
                _ => normalized
            };
        }

        private static IQueryable<Creature> ApplyCreatureSorting(IQueryable<Creature> query, string sort, bool descending)
        {
            return (sort, descending) switch
            {
                ("hitpoints", false) => query.OrderBy(x => x.Hitpoints).ThenBy(x => x.Name),
                ("hitpoints", true) => query.OrderByDescending(x => x.Hitpoints).ThenBy(x => x.Name),
                ("experience", false) => query.OrderBy(x => x.Experience).ThenBy(x => x.Name),
                ("experience", true) => query.OrderByDescending(x => x.Experience).ThenBy(x => x.Name),
                ("last-updated", false) => query.OrderBy(x => x.LastUpdated).ThenBy(x => x.Name),
                ("last-updated", true) => query.OrderByDescending(x => x.LastUpdated).ThenBy(x => x.Name),
                ("name", true) => query.OrderByDescending(x => x.Name),
                _ => query.OrderBy(x => x.Name)
            };
        }

        private static string? GetInfoboxValue(
            IReadOnlyDictionary<string, string>? infobox,
            params string[] keys)
        {
            if(infobox is null)
            {
                return null;
            }

            foreach(string key in keys)
            {
                if(infobox.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }

                KeyValuePair<string, string> match = infobox.FirstOrDefault(entry =>
                    string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));

                if(!string.IsNullOrWhiteSpace(match.Value))
                {
                    return match.Value.Trim();
                }
            }

            return null;
        }

        private static int? ParseNormalizedNumber(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            bool negative = trimmed.StartsWith("-", StringComparison.Ordinal);
            string digits = new(trimmed.Where(char.IsDigit).ToArray());

            if(string.IsNullOrWhiteSpace(digits) || !int.TryParse(digits, out int parsed))
            {
                return null;
            }

            return negative ? -parsed : parsed;
        }

        private static bool? ParseNormalizedBoolean(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "yes" => true,
                "true" => true,
                "1" => true,
                "no" => false,
                "false" => false,
                "0" => false,
                _ => null
            };
        }

        private static decimal? ParseNormalizedDecimal(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim().Replace(',', '.');
            string normalized = new(trimmed.Where(ch => char.IsDigit(ch) || ch is '.' or '-').ToArray());

            return decimal.TryParse(
                normalized,
                System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal parsed)
            ? parsed
            : null;
        }

        private sealed record CachedCreatureDetailsResult(bool Found, CreatureDetailsResponse? Creature)
        {
            public static readonly CachedCreatureDetailsResult NotFound = new(false, null);
        }

        private sealed record CachedCreatureLootResult(bool Found, LootStatisticDetailsResponse? Loot)
        {
            public static readonly CachedCreatureLootResult NotFound = new(false, null);
        }
    }
}
