using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Scraper
{
    public interface IWikiScraper
    {
        string RuntimeScraperName { get; }

        string RuntimeCategorySlug { get; }

        string RuntimeCategoryName { get; }

        Task ExecuteAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            CancellationToken cancellationToken = default);
    }
}