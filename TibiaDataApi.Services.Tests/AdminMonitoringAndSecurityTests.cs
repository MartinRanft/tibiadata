using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

using TibiaDataApi.Services.Admin.Security;
using TibiaDataApi.Services.Admin.Statistics;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Entities.Monitoring;
using TibiaDataApi.Services.Entities.Security;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class AdminMonitoringAndSecurityTests
    {
        [Fact]
        public async Task GetSummaryAsync_ExcludesAdminAndLoopbackTraffic()
        {
            await using ServiceProvider provider = CreateServiceProvider();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            dbContext.ApiRequestLogs.AddRange(
                new ApiRequestLog
                {
                    IpAddress = "203.0.113.10",
                    Method = "GET",
                    Route = "/api/items",
                    StatusCode = 200,
                    DurationMs = 18,
                    OccurredAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new ApiRequestLog
                {
                    IpAddress = "198.51.100.20",
                    Method = "GET",
                    Route = "/api/admin/stats/api",
                    StatusCode = 200,
                    DurationMs = 12,
                    OccurredAt = DateTime.UtcNow.AddMinutes(-8)
                },
                new ApiRequestLog
                {
                    IpAddress = "127.0.0.1",
                    Method = "GET",
                    Route = "/api/items",
                    StatusCode = 200,
                    DurationMs = 9,
                    OccurredAt = DateTime.UtcNow.AddMinutes(-5)
                });

            await dbContext.SaveChangesAsync();

            ApiStatisticsService service = new(
                dbContext,
                scope.ServiceProvider.GetRequiredService<HybridCache>(),
                new CachingOptions
                {
                    UseRedisForHybridCache = false,
                    UseRedisForOutputCache = false
                });

            ApiStatisticsSummary summary = await service.GetSummaryAsync(1);
            ApiRequestLogPage requestLogs = await service.GetRequestLogsAsync();

            Assert.Equal(1, summary.TotalRequests);
            Assert.Single(summary.TopEndpoints);
            Assert.Equal("/api/items", summary.TopEndpoints[0].Route);
            Assert.Single(summary.TopIps);
            Assert.Equal("203.0.113.10", summary.TopIps[0].IpAddress);
            Assert.Single(requestLogs.Items);
            Assert.Equal("203.0.113.10", requestLogs.Items[0].IpAddress);
        }

        [Fact]
        public async Task RecordRequestAsync_PersistsExtendedFields_AndUpdatesDailyAggregate()
        {
            await using ServiceProvider provider = CreateServiceProvider();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            ApiStatisticsService service = new(
                dbContext,
                scope.ServiceProvider.GetRequiredService<HybridCache>(),
                new CachingOptions
                {
                    UseRedisForHybridCache = false,
                    UseRedisForOutputCache = false
                });

            DateTime occurredAt = DateTime.UtcNow.AddMinutes(-15);

            await service.RecordRequestAsync(
                new ApiRequestRecord(
                    "203.0.113.55",
                    "GET",
                    "/api/v1/items/list",
                    200,
                    24.5,
                    "UnitTestAgent/1.0",
                    4096,
                    ApiCacheStatus.Hit,
                    false,
                    occurredAt));

            ApiRequestLog logEntry = await dbContext.ApiRequestLogs.SingleAsync();
            ApiRequestDailyAggregate aggregate = await dbContext.ApiRequestDailyAggregates.SingleAsync();

            Assert.Equal("UnitTestAgent/1.0", logEntry.UserAgent);
            Assert.Equal(4096, logEntry.ResponseSizeBytes);
            Assert.Equal(ApiCacheStatus.Hit, logEntry.CacheStatus);
            Assert.Equal(1, aggregate.RequestCount);
            Assert.Equal(1, aggregate.CacheHitCount);
            Assert.Equal(4096, aggregate.TotalResponseSizeBytes);
            Assert.Equal(occurredAt.Date, aggregate.DayUtc);
        }

        [Fact]
        public async Task GetSummaryAsync_ReportsCacheBreakdown_PeakHour_AndPayloadMetrics()
        {
            await using ServiceProvider provider = CreateServiceProvider();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            DateTime peakHour = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc);

            dbContext.ApiRequestLogs.AddRange(
                new ApiRequestLog
                {
                    IpAddress = "203.0.113.11",
                    Method = "GET",
                    Route = "/api/v1/items/list",
                    StatusCode = 200,
                    DurationMs = 10,
                    ResponseSizeBytes = 1024,
                    CacheStatus = ApiCacheStatus.Hit,
                    OccurredAt = peakHour.AddMinutes(5)
                },
                new ApiRequestLog
                {
                    IpAddress = "203.0.113.12",
                    Method = "GET",
                    Route = "/api/v1/items/list",
                    StatusCode = 429,
                    DurationMs = 5,
                    ResponseSizeBytes = 256,
                    CacheStatus = ApiCacheStatus.Miss,
                    IsBlocked = true,
                    OccurredAt = peakHour.AddMinutes(12)
                },
                new ApiRequestLog
                {
                    IpAddress = "203.0.113.13",
                    Method = "POST",
                    Route = "/api/v1/items/search",
                    StatusCode = 500,
                    DurationMs = 40,
                    ResponseSizeBytes = 128,
                    CacheStatus = ApiCacheStatus.Bypass,
                    OccurredAt = peakHour.AddHours(-2)
                });

            await dbContext.SaveChangesAsync();

            ApiStatisticsService service = new(
                dbContext,
                scope.ServiceProvider.GetRequiredService<HybridCache>(),
                new CachingOptions
                {
                    UseRedisForHybridCache = false,
                    UseRedisForOutputCache = false
                });

            ApiStatisticsSummary summary = await service.GetSummaryAsync(1);

            Assert.Equal(3, summary.TotalRequests);
            Assert.Equal(2, summary.ErrorCount);
            Assert.Equal(1, summary.BlockedCount);
            Assert.Equal(1, summary.CacheHitCount);
            Assert.Equal(1, summary.CacheMissCount);
            Assert.Equal(1, summary.CacheBypassCount);
            Assert.Equal(1408, summary.TotalResponseSizeBytes);
            Assert.Equal(peakHour, summary.PeakRequestHourUtc);
            Assert.Contains(summary.TopStatusCodes, entry => entry.StatusCode == 200 && entry.RequestCount == 1);
            Assert.Contains(summary.TopStatusCodes, entry => entry.StatusCode == 429 && entry.RequestCount == 1);
            Assert.Contains(summary.TopStatusCodes, entry => entry.StatusCode == 500 && entry.RequestCount == 1);
        }

        [Fact]
        public async Task BanAsync_RejectsLoopbackAddresses()
        {
            await using ServiceProvider provider = CreateServiceProvider();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            IpBanService service = new(
                dbContext,
                scope.ServiceProvider.GetRequiredService<HybridCache>(),
                new CachingOptions
                {
                    UseRedisForHybridCache = false,
                    UseRedisForOutputCache = false
                },
                new StubCacheInvalidationService());

            IpBanMutationResult result = await service.BanAsync(
                new IpBanMutationRequest("127.0.0.1", "Test", null, null, "UnitTest"));

            Assert.Equal(IpBanMutationOutcome.ProtectedIp, result.Outcome);
            Assert.False(await dbContext.IpBans.AnyAsync());
        }

        [Fact]
        public async Task BanAsync_PersistsStartEndAndDuration_WhenDurationMinutesIsProvided()
        {
            await using ServiceProvider provider = CreateServiceProvider();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            IpBanService service = new(
                dbContext,
                scope.ServiceProvider.GetRequiredService<HybridCache>(),
                new CachingOptions
                {
                    UseRedisForHybridCache = false,
                    UseRedisForOutputCache = false
                },
                new StubCacheInvalidationService());

            DateTime beforeBan = DateTime.UtcNow;

            IpBanMutationResult result = await service.BanAsync(
                new IpBanMutationRequest("203.0.113.99", "Repeated abuse", null, 90, "AdminDashboard"));

            IpBan storedBan = await dbContext.IpBans.SingleAsync();

            Assert.Equal(IpBanMutationOutcome.Success, result.Outcome);
            Assert.Equal(90, storedBan.DurationMinutes);
            Assert.NotNull(storedBan.ExpiresAt);
            Assert.True(storedBan.ExpiresAt > beforeBan.AddMinutes(89));
            Assert.True(storedBan.ExpiresAt <= beforeBan.AddMinutes(91));
            Assert.Equal("AdminDashboard", storedBan.CreatedBy);
            Assert.True(storedBan.CreatedAt >= beforeBan);
        }

        [Fact]
        public async Task BanAsync_RejectsConflictingExpiryAndDuration()
        {
            await using ServiceProvider provider = CreateServiceProvider();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            IpBanService service = new(
                dbContext,
                scope.ServiceProvider.GetRequiredService<HybridCache>(),
                new CachingOptions
                {
                    UseRedisForHybridCache = false,
                    UseRedisForOutputCache = false
                },
                new StubCacheInvalidationService());

            IpBanMutationResult result = await service.BanAsync(
                new IpBanMutationRequest("203.0.113.101", "Repeated abuse", DateTime.UtcNow.AddHours(2), 30, "AdminDashboard"));

            Assert.Equal(IpBanMutationOutcome.InvalidBanWindow, result.Outcome);
            Assert.False(await dbContext.IpBans.AnyAsync());
        }

        [Fact]
        public async Task IsBlockedAsync_ReturnsFalse_ForLoopbackEvenIfEntryExists()
        {
            await using ServiceProvider provider = CreateServiceProvider();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            dbContext.IpBans.Add(new IpBan
            {
                IpAddress = "127.0.0.1",
                Reason = "Should never apply",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();

            IpBanService service = new(
                dbContext,
                scope.ServiceProvider.GetRequiredService<HybridCache>(),
                new CachingOptions
                {
                    UseRedisForHybridCache = false,
                    UseRedisForOutputCache = false
                },
                new StubCacheInvalidationService());

            bool blocked = await service.IsBlockedAsync("127.0.0.1");

            Assert.False(blocked);
        }

        [Fact]
        public void DatabaseLoadMonitor_AggregatesCommands_AndTracksSlowQueries()
        {
            DatabaseLoadMonitor monitor = new();
            DateTime now = DateTime.UtcNow;

            monitor.RecordCommand("SELECT *   FROM items WHERE id = @__p_0", 18, false, now.AddMinutes(-1));
            monitor.RecordCommand("SELECT * FROM items WHERE id = @__p_0", 420, false, now.AddSeconds(-20));
            monitor.RecordCommand("UPDATE items SET name = @__p_1 WHERE id = @__p_0", 610, true, now.AddSeconds(-5));

            DatabaseLoadSnapshot snapshot = monitor.GetSnapshot();

            Assert.Equal(3, snapshot.TotalCommands);
            Assert.Equal(2, snapshot.TopCommands.Count);
            Assert.Equal(2, snapshot.SlowCommandCount);
            Assert.Equal(1, snapshot.FailedCommandCount);
            Assert.True(snapshot.MaxDurationMs >= 610);
            Assert.Contains(snapshot.RecentSlowCommands, entry => entry.Failed);
            Assert.Contains(snapshot.TopCommands, entry => entry.CommandText.StartsWith("SELECT * FROM items", StringComparison.Ordinal));
        }

        private static ServiceProvider CreateServiceProvider()
        {
            ServiceCollection services = new();

            services.AddLogging();
            services.AddHybridCache();
            services.AddDbContext<TibiaDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")); });

            return services.BuildServiceProvider();
        }

        private sealed class StubCacheInvalidationService : ICacheInvalidationService
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
    }
}
