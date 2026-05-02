using System.ComponentModel.DataAnnotations;

namespace TibiaDataApi.Contracts.Admin
{
    public sealed record AdminScraperStatusResponse(
        string Status,
        bool IsRunning,
        bool StopRequested,
        int? ActiveScrapeLogId,
        IReadOnlyList<int> ActiveScrapeLogIds,
        string? TriggeredBy,
        DateTime? LastStartedAt,
        DateTime? LastFinishedAt,
        string? LastResult,
        string? CurrentScraperName,
        string? CurrentCategoryName,
        string? CurrentCategorySlug,
        int TotalScrapers,
        int CompletedScrapers,
        int ActiveScraperCount,
        IReadOnlyList<AdminActiveScraperItem> ActiveScrapers,
        string? LastMessage,
        string? StopReason);

    public sealed record AdminActiveScraperItem(
        int? ScrapeLogId,
        string ScraperName,
        string CategoryName,
        string CategorySlug,
        DateTime StartedAt);

    public sealed record AdminScraperCatalogResponse(
        IReadOnlyList<AdminScraperCatalogItem> Items);

    public sealed record AdminScraperCatalogItem(
        string ScraperName,
        string CategoryName,
        string CategorySlug);

    public sealed record AdminScraperHistoryResponse(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<AdminScraperHistoryItem> Items);

    public sealed record AdminScraperHistoryItem(
        int ScrapeLogId,
        string Status,
        bool Success,
        string? ScraperName,
        string? CategoryName,
        DateTime StartedAt,
        DateTime? FinishedAt,
        int ItemsProcessed,
        int ItemsAdded,
        int ItemsUpdated,
        int ItemsUnchanged,
        int ItemsFailed,
        int ItemsMissingFromSource,
        string? ErrorType,
        string? ErrorMessage);

    public sealed record AdminScraperChangesResponse(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<AdminScraperChangeItem> Items);

    public sealed record AdminScraperChangeItem(
        int ChangeId,
        int ScrapeLogId,
        string ChangeType,
        string ItemName,
        string? CategoryName,
        DateTime OccurredAt,
        string? ChangedFieldsJson,
        string? ErrorMessage);

    public sealed record AdminScraperErrorsResponse(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<AdminScraperErrorItem> Items);

    public sealed record AdminScraperErrorItem(
        int ErrorId,
        int ScrapeLogId,
        string Scope,
        string ErrorType,
        string Message,
        string? PageTitle,
        string? ItemName,
        DateTime OccurredAt);

    public sealed record AdminScraperRunResponse(
        string Status,
        string Message,
        int? ScrapeLogId);

    public sealed record AdminScraperStopResponse(
        string Status,
        string Message,
        int? ScrapeLogId);

    public sealed record AdminScheduledScraperSettingsResponse(
        bool Enabled,
        int ScheduleHour,
        int ScheduleMinute,
        int TimeoutMinutes,
        DateTime? LastTriggeredAtUtc);

    public sealed record AdminApiStatsResponse(
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
        IReadOnlyList<AdminApiStatBucket> Daily,
        IReadOnlyList<AdminEndpointStatItem> TopEndpoints,
        IReadOnlyList<AdminStatusCodeStatItem> TopStatusCodes,
        IReadOnlyList<AdminIpStatItem> TopIps);

    public sealed record AdminApiStatBucket(
        DateOnly Day,
        int RequestCount,
        int ErrorCount,
        int BlockedCount,
        int CacheHitCount,
        int CacheMissCount,
        int CacheBypassCount,
        long TotalResponseSizeBytes,
        double AverageResponseTimeMs);

    public sealed record AdminEndpointStatItem(
        string Route,
        string Method,
        int RequestCount,
        double AverageResponseTimeMs);

    public sealed record AdminIpStatItem(
        string IpAddress,
        int RequestCount,
        int BlockedCount);

    public sealed record AdminStatusCodeStatItem(
        int StatusCode,
        int RequestCount);

    public sealed record AdminApiRequestLogResponse(
        int Days,
        string? FilteredIpAddress,
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<AdminApiRequestLogItem> Items);

    public sealed record AdminApiRequestLogItem(
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

    public sealed record AdminIpActivityResponse(
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
        IReadOnlyList<AdminEndpointStatItem> TopEndpoints,
        IReadOnlyList<AdminApiRequestLogItem> RecentRequests);

    public sealed record AdminRedisStatusResponse(
        string Status,
        bool UseRedisForHybridCache,
        bool UseRedisForOutputCache,
        string InstanceName,
        string Message,
        DateTime CheckedAtUtc);

    public sealed record AdminMetricsOverviewResponse(
        DateTime CollectedAtUtc,
        int MetricFamilyCount,
        int SampleCount,
        AdminMetricsSummaryResponse Summary,
        IReadOnlyList<AdminMetricSampleResponse> HttpSamples,
        IReadOnlyList<AdminMetricSampleResponse> RuntimeSamples,
        IReadOnlyList<AdminMetricSampleResponse> OtherSamples,
        string RawMetricsText);

    public sealed record AdminMetricsSummaryResponse(
        double? TotalHttpRequests,
        double? HttpRequestsInProgress,
        double? AverageHttpRequestDurationMs,
        double? ProcessWorkingSetMegabytes,
        double? DotNetTotalMemoryMegabytes,
        double? ProcessCpuSecondsTotal,
        double? DotNetGcCollectionsTotal,
        double? DotNetExceptionsTotal);

    public sealed record AdminMetricSampleResponse(
        string Name,
        string? Labels,
        string? Help,
        double Value);

    public sealed record AdminDatabaseLoadResponse(
        DateTime CollectedAtUtc,
        int WindowMinutes,
        int TotalCommands,
        double AverageDurationMs,
        double MaxDurationMs,
        int SlowCommandCount,
        int FailedCommandCount,
        double CommandsPerMinute,
        IReadOnlyList<AdminDatabaseCommandStatItem> TopCommands,
        IReadOnlyList<AdminDatabaseCommandSampleItem> RecentSlowCommands);

    public sealed record AdminDatabaseCommandStatItem(
        string CommandText,
        int Count,
        double AverageDurationMs,
        double MaxDurationMs,
        int FailedCount,
        DateTime LastSeenAtUtc);

    public sealed record AdminDatabaseCommandSampleItem(
        DateTime OccurredAtUtc,
        string CommandText,
        double DurationMs,
        bool Failed);

    public sealed record AdminProtectionRulesResponse(
        IReadOnlyList<AdminProtectionRuleItem> Rules,
        IReadOnlyList<AdminRateLimitPolicyItem> RateLimitPolicies);

    public sealed record AdminRateLimitSettingsResponse(
        bool Enabled,
        int Version,
        DateTime UpdatedAtUtc,
        IReadOnlyList<AdminConfigurableRateLimitPolicyItem> Policies);

    public sealed record AdminProtectionRuleItem(
        string Outcome,
        int StatusCode,
        string Trigger,
        string Effect);

    public sealed record AdminRateLimitPolicyItem(
        string Scope,
        int TokenLimit,
        int TokensPerPeriod,
        int ReplenishmentSeconds,
        int TokenQueueLimit,
        int ConcurrentPermitLimit,
        int ConcurrentQueueLimit);

    public sealed record AdminConfigurableRateLimitPolicyItem(
        string ScopeKey,
        string Scope,
        int TokenLimit,
        int TokensPerPeriod,
        int ReplenishmentSeconds,
        int TokenQueueLimit,
        int ConcurrentPermitLimit,
        int ConcurrentQueueLimit);

    public sealed record AdminIpBanListResponse(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<AdminIpBanItem> Items);

    public sealed record AdminIpBanItem(
        string IpAddress,
        string Reason,
        bool IsActive,
        DateTime StartedAt,
        DateTime? EndsAt,
        int? DurationMinutes,
        string? CreatedBy);

    public sealed record AdminIpBanMutationResponse(
        string Status,
        string Message,
        string IpAddress);

    public sealed record AdminRunScraperRequest(
        bool Force = false,
        [param: MaxLength(200)]string? ScraperName = null,
        [param: MaxLength(100)]string? CategorySlug = null,
        [param: MaxLength(100)]string? TriggeredBy = null);

    public sealed record AdminStopScraperRequest(
        int? ScrapeLogId = null,
        [param: MaxLength(200)]string? ScraperName = null,
        [param: MaxLength(100)]string? CategorySlug = null,
        [param: MaxLength(500)]string? Reason = null,
        [param: MaxLength(100)]string? RequestedBy = null);

    public sealed record AdminUpdateScheduledScraperSettingsRequest(
        bool Enabled,
        [param: Range(0, 23)]int ScheduleHour,
        [param: Range(0, 59)]int ScheduleMinute);

    public sealed record AdminUpdateRateLimitSettingsRequest(
        bool Enabled,
        [param: Required]IReadOnlyList<AdminUpdateRateLimitPolicyItem> Policies);

    public sealed record AdminUpdateRateLimitPolicyItem(
        [param: Required]
        [param: MaxLength(32)]
        string ScopeKey,
        [param: Range(1, 100000)]int TokenLimit,
        [param: Range(1, 100000)]int TokensPerPeriod,
        [param: Range(1, 86400)]int ReplenishmentSeconds,
        [param: Range(0, 100000)]int TokenQueueLimit,
        [param: Range(1, 100000)]int ConcurrentPermitLimit,
        [param: Range(0, 100000)]int ConcurrentQueueLimit);

    public sealed record AdminBanIpRequest(
        [param: Required]
        [param: MaxLength(64)]
        string IpAddress,
        [param: Required]
        [param: MaxLength(500)]
        string Reason,
        DateTime? ExpiresAt = null,
        [param: Range(1, 525600)]int? DurationMinutes = null,
        [param: MaxLength(100)]string? CreatedBy = null);

    public sealed record AdminUnbanIpRequest(
        [param: Required]
        [param: MaxLength(64)]
        string IpAddress,
        [param: MaxLength(500)]string? Reason = null,
        [param: MaxLength(100)]string? RequestedBy = null);
}
