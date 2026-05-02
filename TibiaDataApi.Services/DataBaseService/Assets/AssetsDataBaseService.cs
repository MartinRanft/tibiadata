using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Assets;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.DataBaseService.Assets.Interfaces;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.DataBaseService.Assets
{
    public sealed class AssetsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IAssetsDataBaseService
    {
        
        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };
        
        public async Task<AssetStreamDescriptor?> GetAssetAsync(int assetId, CancellationToken cancellationToken = default)
        {
            if(assetId == 0)
            {
                return null;
            }

            AssetStreamDescriptor? asset = await db.Assets
                                                   .AsNoTracking()
                                                   .Where(x => x.Id == assetId)
                                                   .Select(x => new AssetStreamDescriptor(
                                                       x.StorageKey,
                                                       x.FileName,
                                                       x.MimeType))
                                                   .FirstOrDefaultAsync(cancellationToken);
            return asset;
        }

        public async Task<AssetMetadataResponse?> GetAssetMetaDataAsync(int assetId, CancellationToken cancellationToken = default)
        {
            if(assetId <= 0)
            {
                return null;
            }
            
            return await hybridCache.GetOrCreateAsync(
                $"asset-metadata:{assetId}",
                async ct =>
                {
                    return await db.Assets
                                 .AsNoTracking()
                                 .Where(x => x.Id == assetId)
                                 .Select(x => new AssetMetadataResponse()
                                 {
                                     AssetId = x.Id,
                                     FileName = x.FileName,
                                     MimeType = x.MimeType ?? "application/octet-stream" ,
                                     Hash = x.ContentMd5,
                                     Height = x.Height,
                                     Width = x.Width,
                                     Size = x.SizeBytes
                                 })
                                 .FirstOrDefaultAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Assets, CacheTags.Asset(assetId)],
                cancellationToken);
        }

        public async Task<IReadOnlyList<AssetMetadataResponse>> SearchAssetMetadataByFileNameAsync(
            string fileName,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(fileName))
            {
                return [];
            }

            string normalizedQuery = fileName.Trim().ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                $"asset-metadata-search:{normalizedQuery}",
                async ct =>
                {
                    return await db.Assets
                                   .AsNoTracking()
                                   .Where(x => x.FileName.ToLower().Contains(normalizedQuery))
                                   .OrderBy(x => x.FileName)
                                   .ThenBy(x => x.Id)
                                   .Select(x => new AssetMetadataResponse()
                                   {
                                       AssetId = x.Id,
                                       FileName = x.FileName,
                                       MimeType = x.MimeType ?? "application/octet-stream",
                                       Hash = x.ContentMd5,
                                       Height = x.Height,
                                       Width = x.Width,
                                       Size = x.SizeBytes
                                   })
                                   .Take(25)
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Assets],
                cancellationToken);
        }
    }
}
