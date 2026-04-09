namespace TibiaDataApi.Services.BackgroundJobs
{
    public enum BackgroundJobExecutionState
    {
        Running = 1,
        Completed = 2,
        Failed = 3,
        Skipped = 4,
        Cancelled = 5
    }

    public sealed record BackgroundJobDefinition(
        string JobName,
        string TriggeredBy,
        string? LeaseName = null,
        int TimeoutMinutes = 0,
        int LeaseDurationMinutes = 0,
        int LeaseRenewalSeconds = 0,
        int MaxLeaseRenewalFailures = 0);

    public sealed record BackgroundJobExecutionResult(
        BackgroundJobExecutionState Status,
        string Message,
        int ProcessedCount = 0,
        int SucceededCount = 0,
        int FailedCount = 0,
        int SkippedCount = 0,
        string? MetadataJson = null);

    public sealed record BackgroundJobRunResult(
        bool Executed,
        BackgroundJobExecutionState Status,
        string Message,
        int? ExecutionId,
        BackgroundJobExecutionResult? Result);
}