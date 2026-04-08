namespace TibiaDataApi.Services.DataBaseService.Assets.Interfaces
{
    public interface IAssetsDataBaseService
    {
        Task<AssetStreamDescriptor?> GetAssetAsync(
            int assetId,
            CancellationToken cancellationToken = default);
    }
}