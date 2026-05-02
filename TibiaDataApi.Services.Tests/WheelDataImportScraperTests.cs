using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper.Implementations;
using TibiaDataApi.Services.Scraper.Runtime;
using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.Tests
{
    public sealed class WheelDataImportScraperTests
    {
        [Fact]
        public async Task ExecuteAsync_PersistsImportedCountersIntoScrapeLog()
        {
            string databaseName = Guid.NewGuid().ToString("N");

            await using TibiaDbContext db = CreateDbContext(databaseName);
            ScrapeLog log = new()
            {
                StartedAt = DateTime.UtcNow,
                Status = "Running",
                Success = false,
                ScraperName = "WheelDataImportScraper",
                CategoryName = "Wheel Data Import",
                CategorySlug = "wheel-data-import"
            };

            db.ScrapeLogs.Add(log);
            await db.SaveChangesAsync();

            WheelDataImportScraper scraper = new(
                new StubWheelDataImportService(new WheelDataImportResult(
                    SourceArticleCount: 11,
                    PerksProcessed: 130,
                    Added: 80,
                    Updated: 20,
                    Unchanged: 30,
                    Removed: 5)),
                new StubScraperRuntimeService(),
                NullLogger<WheelDataImportScraper>.Instance);

            await scraper.ExecuteAsync(db, log);

            await using TibiaDbContext verifyDb = CreateDbContext(databaseName);
            ScrapeLog persisted = await verifyDb.ScrapeLogs.SingleAsync();

            Assert.Equal(11, persisted.PagesDiscovered);
            Assert.Equal(11, persisted.PagesProcessed);
            Assert.Equal(130, persisted.ItemsProcessed);
            Assert.Equal(80, persisted.ItemsAdded);
            Assert.Equal(20, persisted.ItemsUpdated);
            Assert.Equal(30, persisted.ItemsUnchanged);
            Assert.Equal(5, persisted.ItemsMissingFromSource);
        }

        private static TibiaDbContext CreateDbContext(string databaseName)
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseInMemoryDatabase(databaseName)
                                                       .Options;

            return new TibiaDbContext(options);
        }

        private sealed class StubWheelDataImportService(WheelDataImportResult result) : IWheelDataImportService
        {
            public Task<WheelDataImportResult> ImportAsync(
                TibiaDbContext db,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(result);
            }
        }

        private sealed class StubScraperRuntimeService : IScraperRuntimeService
        {
            public ScraperRuntimeStatus GetStatus()
            {
                return new ScraperRuntimeStatus(
                    IsRunning: false,
                    StopRequested: false,
                    ActiveScrapeLogId: null,
                    ActiveScrapeLogIds: [],
                    TriggeredBy: null,
                    CurrentScraperName: null,
                    CurrentCategoryName: null,
                    CurrentCategorySlug: null,
                    StartedAt: null,
                    FinishedAt: null,
                    TotalScrapers: 0,
                    CompletedScrapers: 0,
                    ActiveScraperCount: 0,
                    ActiveScrapers: [],
                    LastResult: null,
                    LastMessage: null,
                    StopReason: null);
            }

            public Task<ScraperStartResult> StartAsync(
                ScraperRunRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ScraperStopResult> StopAsync(
                ScraperStopRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ScraperScheduledRunResult> RunScheduledAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
