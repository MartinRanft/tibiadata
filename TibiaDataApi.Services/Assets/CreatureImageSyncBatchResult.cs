namespace TibiaDataApi.Services.Assets
{
    public sealed record CreatureImageSyncBatchResult(
        int Processed,
        int Succeeded,
        int Missing,
        int Failed,
        int Skipped);
}