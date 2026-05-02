using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper.Runtime;
using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.Scraper.Implementations
{
    public sealed class WheelDataImportScraper(
        IWheelDataImportService wheelDataImportService,
        IScraperRuntimeService scraperRuntimeService,
        ILogger<WheelDataImportScraper> logger) : IWikiScraper
    {
        private static readonly TimeSpan PeerWaitPollInterval = TimeSpan.FromMilliseconds(500);

        public string RuntimeScraperName => nameof(WheelDataImportScraper);

        public string RuntimeCategorySlug => "wheel-data-import";

        public string RuntimeCategoryName => "Wheel Data Import";

        public async Task ExecuteAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            CancellationToken cancellationToken = default)
        {
            scrapeLog.ScraperName = RuntimeScraperName;
            scrapeLog.CategoryName = RuntimeCategoryName;
            scrapeLog.CategorySlug = RuntimeCategorySlug;

            await WaitForPeerScrapersAsync(db, scrapeLog, cancellationToken);

            WheelDataImportResult result = await wheelDataImportService.ImportAsync(db, cancellationToken);

            int totalSourcePages = result.SourceArticleCount;
            int totalItemsProcessed = result.PerksProcessed;
            int totalAdded = result.Added;
            int totalUpdated = result.Updated;
            int totalUnchanged = result.Unchanged;
            int totalRemoved = result.Removed;

            if (result.GemImportResult is not null)
            {
                totalSourcePages += result.GemImportResult.SourcePageCount;
                totalItemsProcessed += result.GemImportResult.GemsProcessed + result.GemImportResult.ModifiersProcessed;
                totalAdded += result.GemImportResult.Added;
                totalUpdated += result.GemImportResult.Updated;
                totalUnchanged += result.GemImportResult.Unchanged;
                totalRemoved += result.GemImportResult.Removed;
            }

            scrapeLog.PagesDiscovered = totalSourcePages;
            scrapeLog.PagesProcessed = totalSourcePages;
            scrapeLog.ItemsProcessed = totalItemsProcessed;
            scrapeLog.ItemsAdded = totalAdded;
            scrapeLog.ItemsUpdated = totalUpdated;
            scrapeLog.ItemsUnchanged = totalUnchanged;
            scrapeLog.ItemsMissingFromSource = totalRemoved;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "{ScraperName} finished. Perks: {PerksProcessed}, Gems: {GemsProcessed}, Mods: {ModsProcessed}, Total Added={Added}, Updated={Updated}, Unchanged={Unchanged}, Removed={Removed}",
                RuntimeScraperName,
                result.PerksProcessed,
                result.GemImportResult?.GemsProcessed ?? 0,
                result.GemImportResult?.ModifiersProcessed ?? 0,
                totalAdded,
                totalUpdated,
                totalUnchanged,
                totalRemoved);
        }

        private async Task WaitForPeerScrapersAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            CancellationToken cancellationToken)
        {
            while(true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ScraperRuntimeStatus status = scraperRuntimeService.GetStatus();

                if(!status.IsRunning || status.TotalScrapers <= 1)
                {
                    return;
                }

                
                if(status.ActiveScraperCount <= 1)
                {
                    return;
                }

                bool hasRunningPeers = await db.ScrapeLogs
                                               .AsNoTracking()
                                               .AnyAsync(
                                                   entry => entry.Id != scrapeLog.Id &&
                                                            entry.FinishedAt == null &&
                                                            entry.Status == ScrapeState.Running,
                                                   cancellationToken);

                if(!hasRunningPeers && status.CompletedScrapers >= status.TotalScrapers - 1)
                {
                    return;
                }

                await Task.Delay(PeerWaitPollInterval, cancellationToken);
            }
        }
    }
}
