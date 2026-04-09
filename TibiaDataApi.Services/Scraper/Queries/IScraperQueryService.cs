namespace TibiaDataApi.Services.Scraper.Queries
{
    public interface IScraperQueryService
    {
        Task<ScraperHistoryPage> GetHistoryAsync(
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default);

        Task<ScraperChangesPage> GetChangesAsync(
            int? scrapeLogId = null,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);

        Task<ScraperErrorsPage> GetErrorsAsync(
            int? scrapeLogId = null,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);
    }
}