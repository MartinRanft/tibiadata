using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TibiaDataApi.Services.Admin.Security;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Entities.Security;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class AdminLoginProtectionServiceTests
    {
        [Fact]
        public async Task RegisterFailedAttemptAsync_BansIpAfterFiveFailures()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            AdminLoginProtectionService service = new(dbContext, new NoOpCacheInvalidationService());

            for (int attempt = 1; attempt < 5; attempt++)
            {
                AdminLoginProtectionResult result =
                await service.RegisterFailedAttemptAsync("203.0.113.55");

                Assert.False(result.BanApplied);
                Assert.Equal(attempt, result.FailedAttempts);
            }

            AdminLoginProtectionResult lockoutResult =
            await service.RegisterFailedAttemptAsync("203.0.113.55");

            IpBan? activeBan = await dbContext.IpBans.SingleOrDefaultAsync();
            int remainingFailureEntries = await dbContext.AdminLoginFailures.CountAsync();

            Assert.True(lockoutResult.BanApplied);
            Assert.Equal(5, lockoutResult.FailedAttempts);
            Assert.NotNull(lockoutResult.BanExpiresAt);
            Assert.NotNull(activeBan);
            Assert.Equal(20, activeBan!.DurationMinutes);
            Assert.Equal(lockoutResult.BanExpiresAt, activeBan.ExpiresAt);
            Assert.Equal(0, remainingFailureEntries);
        }

        [Fact]
        public async Task ResetFailuresAsync_RemovesTrackedFailures()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            AdminLoginProtectionService service = new(dbContext, new NoOpCacheInvalidationService());
            await service.RegisterFailedAttemptAsync("203.0.113.80");

            await service.ResetFailuresAsync("203.0.113.80");

            int remainingFailureEntries = await dbContext.AdminLoginFailures.CountAsync();

            Assert.Equal(0, remainingFailureEntries);
        }

        private static TibiaDbContext CreateDbContext(SqliteConnection connection)
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseSqlite(connection)
                                                       .Options;

            return new TibiaDbContext(options);
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
    }
}
