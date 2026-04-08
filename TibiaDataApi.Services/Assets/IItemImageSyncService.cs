namespace TibiaDataApi.Services.Assets
{
    public interface IItemImageSyncService
    {
        Task QueuePrimaryImageSyncAsync(
            int itemId,
            string wikiPageTitle,
            bool forceSync,
            CancellationToken cancellationToken = default);

        Task<ItemImageSyncBatchResult> SyncPendingAsync(
            int batchSize,
            CancellationToken cancellationToken = default);
    }
}