using Microsoft.EntityFrameworkCore;

using TibiaDataApi.Services.DataBaseService.Assets.Interfaces;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.DataBaseService.Assets
{
    public sealed class AssetsDataBaseService(TibiaDbContext db) : IAssetsDataBaseService
    {
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
    }
}