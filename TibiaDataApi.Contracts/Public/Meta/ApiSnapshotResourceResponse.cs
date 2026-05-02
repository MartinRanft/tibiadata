namespace TibiaDataApi.Contracts.Public.Meta
{
    public sealed record ApiSnapshotResourceResponse(
        string Key,
        string DisplayName,
        int Count,
        DateTime? LatestUpdateUtc,
        string? ListRoute,
        string? DetailByNameRoutePattern,
        string? DetailByIdRoutePattern,
        string? SyncRoute,
        string? SyncByDateRoute,
        IReadOnlyList<string> RelatedRoutes);
}
