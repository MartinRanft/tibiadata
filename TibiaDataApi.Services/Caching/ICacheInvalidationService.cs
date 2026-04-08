namespace TibiaDataApi.Services.Caching
{
    public interface ICacheInvalidationService
    {
        Task InvalidateIpBansAsync(string? ipAddress = null, CancellationToken cancellationToken = default);
        Task InvalidateScraperQueriesAsync(int? scrapeLogId = null, CancellationToken cancellationToken = default);
        Task InvalidateScrapedContentAsync(string? categorySlug = null, CancellationToken cancellationToken = default);
    }
}