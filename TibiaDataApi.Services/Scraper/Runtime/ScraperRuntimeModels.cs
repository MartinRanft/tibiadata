namespace TibiaDataApi.Services.Scraper.Runtime
{
    public sealed record ScraperRunRequest(
        bool Force = false,
        string? ScraperName = null,
        string? CategorySlug = null,
        string? TriggeredBy = null);

    public sealed record ScraperStopRequest(
        string? Reason = null,
        string? RequestedBy = null);

    public sealed record ActiveScraperRuntimeStatus(
        int? ScrapeLogId,
        string ScraperName,
        string CategoryName,
        string CategorySlug,
        DateTime StartedAt);

    public sealed record ScraperRuntimeStatus(
        bool IsRunning,
        bool StopRequested,
        int? ActiveScrapeLogId,
        IReadOnlyList<int> ActiveScrapeLogIds,
        string? TriggeredBy,
        string? CurrentScraperName,
        string? CurrentCategoryName,
        string? CurrentCategorySlug,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        int TotalScrapers,
        int CompletedScrapers,
        int ActiveScraperCount,
        IReadOnlyList<ActiveScraperRuntimeStatus> ActiveScrapers,
        string? LastResult,
        string? LastMessage,
        string? StopReason);

    public sealed record ScraperStartResult(
        bool Started,
        string Message,
        ScraperRuntimeStatus Status);

    public sealed record ScraperStopResult(
        bool StopRequested,
        string Message,
        ScraperRuntimeStatus Status);

    public sealed record ScraperScheduledRunResult(
        bool Triggered,
        string Message,
        ScraperRuntimeStatus Status);
}