namespace TibiaDataApi.Services.Admin.Statistics
{
    public sealed record ApiRequestRecord(
        string IpAddress,
        string Method,
        string Route,
        int StatusCode,
        double DurationMs,
        string? UserAgent,
        long ResponseSizeBytes,
        string CacheStatus,
        bool IsBlocked,
        DateTime OccurredAt);

    public sealed record ApiStatisticsSummary(
        int Days,
        int TotalRequests,
        int UniqueIpCount,
        int ErrorCount,
        int BlockedCount,
        double AverageResponseTimeMs,
        long TotalResponseSizeBytes,
        double AverageResponseSizeBytes,
        int CacheHitCount,
        int CacheMissCount,
        int CacheBypassCount,
        DateTime? PeakRequestHourUtc,
        IReadOnlyList<ApiDailyStatBucket> Daily,
        IReadOnlyList<ApiEndpointStat> TopEndpoints,
        IReadOnlyList<ApiStatusCodeStat> TopStatusCodes,
        IReadOnlyList<ApiIpStat> TopIps);

    public sealed record ApiDailyStatBucket(
        DateOnly Day,
        int RequestCount,
        int ErrorCount,
        int BlockedCount,
        int CacheHitCount,
        int CacheMissCount,
        int CacheBypassCount,
        long TotalResponseSizeBytes,
        double AverageResponseTimeMs);

    public sealed record ApiEndpointStat(
        string Route,
        string Method,
        int RequestCount,
        double AverageResponseTimeMs);

    public sealed record ApiIpStat(
        string IpAddress,
        int RequestCount,
        int BlockedCount);

    public sealed record ApiStatusCodeStat(
        int StatusCode,
        int RequestCount);

    public sealed record ApiRequestLogPage(
        int Days,
        string? FilteredIpAddress,
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<ApiRequestLogEntry> Items);

    public sealed record ApiRequestLogEntry(
        int RequestId,
        string IpAddress,
        string Method,
        string Route,
        int StatusCode,
        double DurationMs,
        string? UserAgent,
        long ResponseSizeBytes,
        string CacheStatus,
        bool IsBlocked,
        DateTime OccurredAt);

    public sealed record ApiIpActivityDetails(
        string IpAddress,
        int Days,
        int TotalRequests,
        int BlockedCount,
        int ErrorCount,
        double AverageResponseTimeMs,
        long TotalResponseSizeBytes,
        double AverageResponseSizeBytes,
        DateTime? FirstSeenAt,
        DateTime? LastSeenAt,
        IReadOnlyList<ApiEndpointStat> TopEndpoints,
        IReadOnlyList<ApiRequestLogEntry> RecentRequests);
}
