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

        private sealed record CachedCreatureDetailsResult(bool Found, CreatureDetailsResponse? Creature)
        {
            public static readonly CachedCreatureDetailsResult NotFound = new(false, null);
        }
    }
}