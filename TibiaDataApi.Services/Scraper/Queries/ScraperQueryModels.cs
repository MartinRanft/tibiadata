namespace TibiaDataApi.Services.Scraper.Queries
{
    public sealed record ScraperHistoryPage(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<ScraperHistoryEntry> Items);

    public sealed record ScraperHistoryEntry(
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

    public sealed record ScraperChangesPage(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<ScraperChangeEntry> Items);

    public sealed record ScraperChangeEntry(
        int ChangeId,
        int ScrapeLogId,
        string ChangeType,
        string ItemName,
        string? CategoryName,
        DateTime OccurredAt,
        string? ChangedFieldsJson,
        string? ErrorMessage);

    public sealed record ScraperErrorsPage(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<ScraperErrorEntry> Items);

    public sealed record ScraperErrorEntry(
        int ErrorId,
        int ScrapeLogId,
        string Scope,
        string ErrorType,
        string Message,
        string? PageTitle,
        string? ItemName,
        DateTime OccurredAt);
}