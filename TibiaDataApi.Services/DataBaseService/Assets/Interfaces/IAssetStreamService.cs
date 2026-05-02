namespace TibiaDataApi.Services.DataBaseService.Assets.Interfaces
{
    public interface IAssetStreamService
    {
        Task<AssetStreamResult?> OpenReadAsync(
            int assetId,
            CancellationToken cancellationToken = default);
    }
}