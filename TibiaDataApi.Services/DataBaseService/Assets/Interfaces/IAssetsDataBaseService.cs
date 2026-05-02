using TibiaDataApi.Contracts.Public.Assets;

namespace TibiaDataApi.Services.DataBaseService.Assets.Interfaces
{
    public interface IAssetsDataBaseService
    {
        Task<AssetStreamDescriptor?> GetAssetAsync(
            int assetId,
            CancellationToken cancellationToken = default);
        Task<AssetMetadataResponse?> GetAssetMetaDataAsync(
            int assetId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AssetMetadataResponse>> SearchAssetMetadataByFileNameAsync(
            string fileName,
            CancellationToken cancellationToken = default);
    }
}
