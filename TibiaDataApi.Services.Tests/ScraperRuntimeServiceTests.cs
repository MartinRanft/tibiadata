using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper;
using TibiaDataApi.Services.Scraper.Runtime;

namespace TibiaDataApi.Services.Tests
{
    public sealed class ScraperRuntimeServiceTests
    {
        [Fact]
        public async Task StartAsync_RunsAllRegisteredScrapersInParallel()
        {
            ScraperExecutionSignal signal = new(3);
            ServiceProvider serviceProvider = CreateServiceProvider(Guid.NewGuid().ToString("N"), signal, 3);
            ScraperRuntimeService runtimeService = CreateRuntimeService(serviceProvider);

            ScraperStartResult result = await runtimeService.StartAsync(new ScraperRunRequest(TriggeredBy: "Test"));

            Assert.True(result.Started);
            await signal.WaitForAllStartedAsync().WaitAsync(TimeSpan.FromSeconds(5));

            ScraperRuntimeStatus runningStatus = runtimeService.GetStatus();

            Assert.True(runningStatus.IsRunning);
            Assert.Equal(3, runningStatus.ActiveScraperCount);
            Assert.Equal(3, signal.MaxConcurrentObserved);

            signal.ReleaseAll();
            await WaitUntilAsync(() => !runtimeService.GetStatus().IsRunning);

            ScraperRuntimeStatus finalStatus = runtimeService.GetStatus();

            Assert.Equal(ScrapeState.Completed, finalStatus.LastResult);
            Assert.Equal(3, finalStatus.CompletedScrapers);
            Assert.Empty(finalStatus.ActiveScrapers);
        }

        [Fact]
        public async Task StopAsync_CancelsAllActiveScraperTasks()
        {
            ScraperExecutionSignal signal = new(2);
            string databaseName = Guid.NewGuid().ToString("N");
            ServiceProvider serviceProvider = CreateServiceProvider(databaseName, signal, 2);
            ScraperRuntimeService runtimeService = CreateRuntimeService(serviceProvider);

            ScraperStartResult startResult = await runtimeService.StartAsync(new ScraperRunRequest(TriggeredBy: "Test"));

            Assert.True(startResult.Started);
            await signal.WaitForAllStartedAsync().WaitAsync(TimeSpan.FromSeconds(5));

            ScraperStopResult stopResult = await runtimeService.StopAsync(new ScraperStopRequest("Admin stop", "Test"));

            Assert.True(stopResult.StopRequested);

            await WaitUntilAsync(() => !runtimeService.GetStatus().IsRunning);

            ScraperRuntimeStatus finalStatus = runtimeService.GetStatus();

            Assert.Equal(ScrapeState.Cancelled, finalStatus.LastResult);

            await using AsyncServiceScope verifyScope = serviceProvider.CreateAsyncScope();
            TibiaDbContext db = verifyScope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            List<ScrapeLog> logs = await db.ScrapeLogs.OrderBy(entry => entry.Id).ToListAsync();

            Assert.Equal(2, logs.Count);
            Assert.All(logs, log => Assert.Equal(ScrapeState.Cancelled, log.Status));
            Assert.All(logs, log => Assert.Contains("Admin stop", log.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        private static ScraperRuntimeService CreateRuntimeService(IServiceProvider serviceProvider)
        {
            return new ScraperRuntimeService(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                new NoOpCacheInvalidationService(),
                new ScraperRuntimeOptions
                {
                    RunTimeoutMinutes = 5,
                    MaxConcurrentScrapers = 0
                },
                new TestScraperExecutionLeaseService(),
                NullLogger<ScraperRuntimeService>.Instance);
        }

        private static ServiceProvider CreateServiceProvider(
            string databaseName,
            ScraperExecutionSignal signal,
            int scraperCount)
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddDbContext<TibiaDbContext>(options => options.UseInMemoryDatabase(databaseName));

            for (int index = 1; index <= scraperCount; index++)
            {
                int localIndex = index;
                services.AddTransient<IWikiScraper>(_ => new BlockingTestScraper(
                    $"TestScraper{localIndex}",
                    $"Category {localIndex}",
                    $"category-{localIndex}",
                    signal));
            }

            return services.BuildServiceProvider();
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(5);

            while (DateTime.UtcNow < deadline)
            {
                if(condition())
                {
                    return;
                }

                await Task.Delay(50);
            }

            throw new TimeoutException("The expected scraper runtime state was not reached in time.");
        }

        private sealed class BlockingTestScraper(
            string scraperName,
            string categoryName,
            string categorySlug,
            ScraperExecutionSignal signal) : IWikiScraper
        {
            public string RuntimeScraperName => scraperName;

            public string RuntimeCategorySlug => categorySlug;

            public string RuntimeCategoryName => categoryName;

            public async Task ExecuteAsync(
                TibiaDbContext db,
                ScrapeLog scrapeLog,
                CancellationToken cancellationToken = default)
            {
                signal.MarkStarted(RuntimeScraperName);

                try
                {
                    await signal.WaitForReleaseAsync(cancellationToken);
                }
                finally
                {
                    signal.MarkFinished(RuntimeScraperName);
                }
            }
        }

        private sealed class ScraperExecutionSignal(int expectedCount)
        {
            private readonly TaskCompletionSource _allStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int _activeCount;
            private int _maxConcurrentObserved;
            private int _startedCount;

            public int MaxConcurrentObserved => _maxConcurrentObserved;

            public void MarkStarted(string scraperName)
            {
                int activeCount = Interlocked.Increment(ref _activeCount);
                InterlockedExtensions.Max(ref _maxConcurrentObserved, activeCount);

                if(Interlocked.Increment(ref _startedCount) >= expectedCount)
                {
                    _allStarted.TrySetResult();
                }
            }

            public void MarkFinished(string scraperName)
            {
                Interlocked.Decrement(ref _activeCount);
            }

            public Task WaitForAllStartedAsync()
            {
                return _allStarted.Task;
            }

            public Task WaitForReleaseAsync(CancellationToken cancellationToken)
            {
                return _release.Task.WaitAsync(cancellationToken);
            }

            public void ReleaseAll()
            {
                _release.TrySetResult();
            }
        }

        private sealed class NoOpCacheInvalidationService : ICacheInvalidationService
        {
            public Task InvalidateIpBansAsync(string? ipAddress = null, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task InvalidateScraperQueriesAsync(int? scrapeLogId = null, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task InvalidateScrapedContentAsync(string? categorySlug = null, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class TestScraperExecutionLeaseService : IScraperExecutionLeaseService
        {
            public Task<ScraperExecutionLeaseAcquireResult> TryAcquireAsync(
                string leaseName,
                string ownerId,
                TimeSpan leaseDuration,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ScraperExecutionLeaseAcquireResult(true, ownerId, DateTime.UtcNow.Add(leaseDuration)));
            }

            public Task<bool> RenewAsync(
                string leaseName,
                string ownerId,
                TimeSpan leaseDuration,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(true);
            }

            public Task ReleaseAsync(
                string leaseName,
                string ownerId,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private static class InterlockedExtensions
        {
            public static void Max(ref int target, int value)
            {
                int snapshot;

                do
                {
                    snapshot = target;

                    if(snapshot >= value)
                    {
                        return;
                    }
                } while (Interlocked.CompareExchange(ref target, value, snapshot) != snapshot);
            }
        }
    }
}