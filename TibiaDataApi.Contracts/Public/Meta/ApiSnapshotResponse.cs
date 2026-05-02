namespace TibiaDataApi.Contracts.Public.Meta
{
    public sealed record ApiSnapshotResponse(
        int SchemaVersion,
        string DataVersion,
        DateTime GeneratedAtUtc,
        DateTime? LatestDataUpdateUtc,
        IReadOnlyList<ApiSnapshotResourceResponse> Resources);
}
