namespace TibiaDataApi.Services.Assets
{
    public interface ICreatureImageSyncService
    {
        Task QueuePrimaryImageSyncAsync(
            int creatureId,
            string wikiPageTitle,
            bool forceSync,
            CancellationToken cancellationToken = default);

        Task<CreatureImageSyncBatchResult> SyncPendingAsync(
            int batchSize,
            CancellationToken cancellationToken = default);
    }
}