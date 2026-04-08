using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TibiaDataApi.Services.BackgroundJobs;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class ScheduledScraperConfigurationServiceTests
    {
        [Fact]
        public async Task GetAsync_ReturnsConfiguredDefaults_WhenDatabaseRowDoesNotExist()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            ScheduledScraperConfigurationService service = CreateService(dbContext);

            ScheduledScraperSchedule schedule = await service.GetAsync();

            Assert.True(schedule.Enabled);
            Assert.Equal(3, schedule.ScheduleHour);
            Assert.Equal(15, schedule.ScheduleMinute);
            Assert.Equal(25, schedule.TimeoutMinutes);
            Assert.Null(schedule.LastTriggeredAtUtc);
        }

        [Fact]
        public async Task UpdateAsync_PersistsConfiguredValues()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            ScheduledScraperConfigurationService service = CreateService(dbContext);

            ScheduledScraperSchedule updated = await service.UpdateAsync(false, 6, 45);
            ScheduledScraperSchedule persisted = await service.GetAsync();

            Assert.False(updated.Enabled);
            Assert.Equal(6, updated.ScheduleHour);
            Assert.Equal(45, updated.ScheduleMinute);
            Assert.False(persisted.Enabled);
            Assert.Equal(6, persisted.ScheduleHour);
            Assert.Equal(45, persisted.ScheduleMinute);
        }

        [Fact]
        public async Task ShouldRunAtAsync_ReturnsTrue_AtConfiguredTime_WhenNotTriggeredToday()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            ScheduledScraperConfigurationService service = CreateService(dbContext);
            await service.UpdateAsync(true, 4, 20);

            bool shouldRun = await service.ShouldRunAtAsync(new DateTime(2026, 4, 6, 4, 20, 0, DateTimeKind.Local));

            Assert.True(shouldRun);
        }

        [Fact]
        public async Task ShouldRunAtAsync_ReturnsFalse_WhenAlreadyTriggeredOnSameLocalDay()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            ScheduledScraperConfigurationService service = CreateService(dbContext);
            DateTime localRunTime = new(2026, 4, 6, 4, 20, 0, DateTimeKind.Local);

            await service.UpdateAsync(true, 4, 20);
            await service.MarkTriggeredAsync(localRunTime.ToUniversalTime());

            bool shouldRun = await service.ShouldRunAtAsync(localRunTime);

            Assert.False(shouldRun);
        }

        private static ScheduledScraperConfigurationService CreateService(TibiaDbContext dbContext)
        {
            return new ScheduledScraperConfigurationService(
                dbContext,
                new BackgroundJobOptions
                {
                    ScheduledScraper = new ScheduledScraperJobOptions
                    {
                        Enabled = true,
                        ScheduleHour = 3,
                        ScheduleMinute = 15,
                        TimeoutMinutes = 25
                    }
                });
        }

        private static TibiaDbContext CreateDbContext(SqliteConnection connection)
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseSqlite(connection)
                                                       .Options;

            return new TibiaDbContext(options);
        }
    }
}