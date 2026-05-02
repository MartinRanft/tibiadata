namespace TibiaDataApi.Contracts.Public.Common
{
    public sealed record SyncStateResponse(
        int Id,
        DateTime LastUpdated,
        DateTime? LastSeenAt);
}