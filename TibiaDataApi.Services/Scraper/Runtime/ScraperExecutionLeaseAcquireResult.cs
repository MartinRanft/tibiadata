namespace TibiaDataApi.Services.Scraper.Runtime
{
    public sealed record ScraperExecutionLeaseAcquireResult(
        bool Acquired,
        string? CurrentOwnerId,
        DateTime? ExpiresAt);
}