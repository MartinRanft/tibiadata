using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.DataBaseService.Assets.Interfaces;

namespace TibiaDataApi.Services.DataBaseService.Assets
{
    public sealed class AssetStreamService(IAssetsDataBaseService assetsDataBaseService, AssetStorageOptions options) : IAssetStreamService
    {
        public async Task<AssetStreamResult?> OpenReadAsync(int assetId, CancellationToken cancellationToken = default)
        {
            AssetStreamDescriptor? descriptor = await assetsDataBaseService.GetAssetAsync(assetId, cancellationToken);

            if(descriptor is null)
            {
                return null;
            }

            string fullPath = Path.Combine(options.StorageRootPath, descriptor.StorageKey);
            Stream stream = File.OpenRead(fullPath);

            return new AssetStreamResult(stream, descriptor.FileName, descriptor.MimeType);
        }
    }
}