namespace TibiaDataApi.Contracts.Public.Meta
{
    public sealed record ApiDeltaFeedResponse(
        int SchemaVersion,
        string DataVersion,
        DateTime GeneratedAtUtc,
        DateTime SinceUtc,
        DateTime? LatestChangeUtc,
        int ReturnedCount,
        bool HasMore,
        IReadOnlyList<ApiDeltaEntryResponse> Changes);
}
