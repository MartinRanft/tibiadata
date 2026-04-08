using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Items;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Items.Interfaces;
using TibiaDataApi.Services.Entities.Assets;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Items
{
    public sealed class ItemsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IItemsDataBaseService
    {
        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<List<string>> GetItemNamesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "items:names",
                async cancellationToken =>
                {
                    return await db.Items
                                   .AsNoTracking()
                                   .Where(x => !x.IsMissingFromSource)
                                   .OrderBy(x => x.Name)
                                   .Select(x => x.Name)
                                   .ToListAsync(cancellationToken);
                },
                _cacheOptions,
                [CacheTags.Items],
                cancellationToken);
        }

        public async Task<PagedResponse<ItemListItemResponse>> GetItemsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            int normalizedPage = page < 1 ? 1 : page;
            int normalizedPageSize = Math.Clamp(pageSize, 1, 100);

            return await hybridCache.GetOrCreateAsync(
                $"items:list:{normalizedPage}:{normalizedPageSize}",
                async cancellationToken =>
                {
                    IQueryable<Item> query = db.Items
                                               .AsNoTracking()
                                               .Where(x => !x.IsMissingFromSource);

                    int totalCount = await query.CountAsync(cancellationToken);
                    IQueryable<Item> pagedQuery = query
                                                  .OrderBy(x => x.Name)
                                                  .Skip((normalizedPage - 1) * normalizedPageSize)
                                                  .Take(normalizedPageSize);

                    List<ItemListItemResponse> items = await ProjectItemListItems(pagedQuery)
                    .ToListAsync(cancellationToken);

                    return new PagedResponse<ItemListItemResponse>(normalizedPage, normalizedPageSize, totalCount, items);
                },
                _cacheOptions,
                [CacheTags.Items],
                cancellationToken);
        }

        public async Task<ItemDetailsResponse?> GetItemByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(name);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            CachedItemDetailsResult cachedResult = await hybridCache.GetOrCreateAsync(
                $"items:by-name:{normalizedName}",
                async cancellationToken =>
                {
                    int exactId = await db.Items
                                          .AsNoTracking()
                                          .Where(x => !x.IsMissingFromSource)
                                          .Where(x => x.NormalizedName == normalizedName)
                                          .Select(x => x.Id)
                                          .SingleOrDefaultAsync(cancellationToken);

                    if(exactId > 0)
                    {
                        ItemDetailsResponse? item = await GetItemByIdAsync(exactId, cancellationToken);
                        return new CachedItemDetailsResult(item is not null, item);
                    }

                    List<int> actualNameMatches = await db.Items
                                                          .AsNoTracking()
                                                          .Where(x => !x.IsMissingFromSource)
                                                          .Where(x => x.NormalizedActualName == normalizedName)
                                                          .OrderBy(x => x.Name)
                                                          .Select(x => x.Id)
                                                          .Take(2)
                                                          .ToListAsync(cancellationToken);

                    if(actualNameMatches.Count != 1)
                    {
                        return CachedItemDetailsResult.NotFound;
                    }

                    ItemDetailsResponse? itemByActualName = await GetItemByIdAsync(actualNameMatches[0], cancellationToken);
                    return new CachedItemDetailsResult(itemByActualName is not null, itemByActualName);
                },
                _cacheOptions,
                [CacheTags.Items],
                cancellationToken);

            return cachedResult.Item;
        }

        public async Task<ItemDetailsResponse?> GetItemByIdAsync(int? id, CancellationToken cancellationToken = default)
        {
            if(id is <= 0 or null)
            {
                return null;
            }

            CachedItemDetailsResult cachedResult = await hybridCache.GetOrCreateAsync(
                $"items:by-id:{id}",
                async cancellationToken =>
                {
                    Item? item = await db.Items
                                         .AsNoTracking()
                                         .Include(x => x.Category)
                                         .Include(x => x.ItemAssets)
                                         .ThenInclude(x => x.Asset)
                                         .FirstOrDefaultAsync(
                                             x => x.Id == id && !x.IsMissingFromSource,
                                             cancellationToken);

                    return item is null
                    ? CachedItemDetailsResult.NotFound
                    : new CachedItemDetailsResult(true, MapItemDetails(item));
                },
                _cacheOptions,
                [CacheTags.Items],
                cancellationToken);

            return cachedResult.Item;
        }

        public async Task<List<string>> GetItemCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "items:categories",
                async cancellationToken =>
                {
                    return await db.WikiCategories
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.Item)
                                   .Where(x => x.IsActive)
                                   .Where(x => db.Items.Any(item => item.CategoryId == x.Id && !item.IsMissingFromSource))
                                   .OrderBy(x => x.SortOrder)
                                   .ThenBy(x => x.Name)
                                   .Select(x => x.Slug)
                                   .ToListAsync(cancellationToken);
                },
                _cacheOptions,
                [CacheTags.Items, CacheTags.Categories],
                cancellationToken);
        }

        public async Task<List<ItemListItemResponse>> GetItemsByCategoryAsync(
            string categorySlug,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            string normalizedCategorySlug = NormalizeCategorySlug(categorySlug);
            if(string.IsNullOrWhiteSpace(normalizedCategorySlug))
            {
                return [];
            }

            int normalizedPage = page < 1 ? 1 : page;
            int normalizedPageSize = Math.Clamp(pageSize, 1, 100);

            return await hybridCache.GetOrCreateAsync(
                $"items:category:{normalizedCategorySlug}:{normalizedPage}:{normalizedPageSize}",
                async cancellationToken =>
                {
                    IQueryable<Item> query = db.Items
                                               .AsNoTracking()
                                               .Where(x => !x.IsMissingFromSource)
                                               .Where(x => x.Category != null && x.Category.Slug == normalizedCategorySlug);
                    IQueryable<Item> pagedQuery = query
                                                  .OrderBy(x => x.Name)
                                                  .Skip((normalizedPage - 1) * normalizedPageSize)
                                                  .Take(normalizedPageSize);

                    return await ProjectItemListItems(pagedQuery)
                    .ToListAsync(cancellationToken);
                },
                _cacheOptions,
                [CacheTags.Items, CacheTags.Categories, CacheTags.Category(normalizedCategorySlug)],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetItemUpdates(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "items:updates",
                async ct =>
                {
                    return await db.Items
                                   .AsNoTracking()
                                   .OrderBy(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Items],
                cancellationToken);
        }
        public async Task<List<SyncStateResponse>> GetItemUpdatesByDate(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "items:updates-by-date",
                async ct =>
                {
                    return await db.Items
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
                [CacheTags.Items],
                cancellationToken);
        }

        private static ItemDetailsResponse MapItemDetails(Item item)
        {
            List<ItemImageResponse> images = item.ItemAssets
                                                 .Where(x => x.Asset is not null)
                                                 .OrderByDescending(x => x.IsPrimary)
                                                 .ThenBy(x => x.SortOrder)
                                                 .Select(x => MapImage(x.AssetId, x.Asset!))
                                                 .ToList();

            return new ItemDetailsResponse(
                item.Id,
                item.Name,
                item.ActualName,
                item.Plural,
                item.Article,
                item.Implemented,
                item.ItemId,
                item.DroppedBy,
                item.Sounds,
                item.Category?.Slug,
                item.Category?.Name,
                item.TemplateType,
                item.ObjectClass,
                item.PrimaryType,
                item.SecondaryType,
                item.WeaponType,
                item.Hands,
                item.Attack,
                item.Defense,
                item.DefenseMod,
                item.Armor,
                item.Range,
                item.LevelRequired,
                item.ImbueSlots,
                item.Vocation,
                item.DamageType,
                item.DamageRange,
                item.EnergyAttack,
                item.FireAttack,
                item.EarthAttack,
                item.IceAttack,
                item.DeathAttack,
                item.HolyAttack,
                item.Stackable,
                item.Usable,
                item.Marketable,
                item.Walkable,
                item.NpcPrice,
                item.NpcValue,
                item.Value,
                item.Weight,
                item.Attrib,
                item.UpgradeClass,
                item.WikiUrl,
                MapAdditionalAttributes(item.AdditionalAttributesJson),
                item.LastSeenAt,
                item.LastUpdated,
                images);
        }

        private static ItemImageResponse MapImage(int assetId, Asset asset)
        {
            return new ItemImageResponse(
                assetId,
                asset.StorageKey,
                asset.FileName,
                asset.MimeType,
                asset.Width,
                asset.Height);
        }

        private static ItemAdditionalAttributesResponse? MapAdditionalAttributes(string? additionalAttributesJson)
        {
            if(string.IsNullOrWhiteSpace(additionalAttributesJson))
            {
                return null;
            }

            try
            {
                Dictionary<string, string>? values = JsonSerializer.Deserialize<Dictionary<string, string>>(additionalAttributesJson);
                if(values is null || values.Count == 0)
                {
                    return null;
                }

                List<ItemAttributeEntryResponse> entries = values
                                                           .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                                                           .Select(x => new ItemAttributeEntryResponse(x.Key, x.Value))
                                                           .OrderBy(x => x.Key)
                                                           .ToList();

                return entries.Count == 0 ? null : new ItemAdditionalAttributesResponse(entries);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static IQueryable<ItemListItemResponse> ProjectItemListItems(IQueryable<Item> query)
        {
            return query.Select(x => new ItemListItemResponse(
                x.Id,
                x.Name,
                x.Category != null ? x.Category.Slug : null,
                x.Category != null ? x.Category.Name : null,
                x.PrimaryType,
                x.SecondaryType,
                x.ObjectClass,
                x.WikiUrl,
                x.LastUpdated,
                x.ItemAssets
                 .OrderByDescending(asset => asset.IsPrimary)
                 .ThenBy(asset => asset.SortOrder)
                 .Select(asset => asset.Asset == null
                 ? null
                 : new ItemImageResponse(
                     asset.AssetId,
                     asset.Asset.StorageKey,
                     asset.Asset.FileName,
                     asset.Asset.MimeType,
                     asset.Asset.Width,
                     asset.Asset.Height))
                 .FirstOrDefault()));
        }

        private static string NormalizeCategorySlug(string categorySlug)
        {
            return string.IsNullOrWhiteSpace(categorySlug)
            ? string.Empty
            : categorySlug.Trim().ToLowerInvariant();
        }

        private sealed record CachedItemDetailsResult(bool Found, ItemDetailsResponse? Item)
        {
            public static readonly CachedItemDetailsResult NotFound = new(false, null);
        }
    }
}