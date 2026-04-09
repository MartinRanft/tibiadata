using Microsoft.EntityFrameworkCore;

using TibiaDataApi.Services.Entities.Monitoring;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.BackgroundJobs
{
    public sealed class ScheduledScraperConfigurationService(
        TibiaDbContext dbContext,
        BackgroundJobOptions backgroundJobOptions) : IScheduledScraperConfigurationService
    {
        private const string PrimaryConfigurationKey = "scheduled-scraper";

        public async Task<ScheduledScraperSchedule> GetAsync(CancellationToken cancellationToken = default)
        {
            ScheduledScraperConfiguration? configuration = await dbContext.ScheduledScraperConfigurations
                                                                          .AsNoTracking()
                                                                          .SingleOrDefaultAsync(
                                                                              entry => entry.Key == PrimaryConfigurationKey,
                                                                              cancellationToken);

            return configuration is null
            ? CreateDefaultSchedule()
            : MapSchedule(configuration);
        }

        public async Task<ScheduledScraperSchedule> UpdateAsync(
            bool enabled,
            int scheduleHour,
            int scheduleMinute,
            CancellationToken cancellationToken = default)
        {
            ScheduledScraperConfiguration configuration =
            await GetOrCreateConfigurationAsync(cancellationToken);

            configuration.Enabled = enabled;
            configuration.ScheduleHour = Math.Clamp(scheduleHour, 0, 23);
            configuration.ScheduleMinute = Math.Clamp(scheduleMinute, 0, 59);
            configuration.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return MapSchedule(configuration);
        }

        public async Task<bool> ShouldRunAtAsync(DateTime localNow, CancellationToken cancellationToken = default)
        {
            ScheduledScraperSchedule schedule = await GetAsync(cancellationToken);

            if(!schedule.Enabled)
            {
                return false;
            }

            if(localNow.Hour != schedule.ScheduleHour || localNow.Minute != schedule.ScheduleMinute)
            {
                return false;
            }

            return schedule.LastTriggeredAtUtc?.ToLocalTime().Date != localNow.Date;
        }

        public async Task MarkTriggeredAsync(DateTime triggeredAtUtc, CancellationToken cancellationToken = default)
        {
            ScheduledScraperConfiguration configuration =
            await GetOrCreateConfigurationAsync(cancellationToken);

            configuration.LastTriggeredAtUtc = triggeredAtUtc;
            configuration.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<ScheduledScraperConfiguration> GetOrCreateConfigurationAsync(CancellationToken cancellationToken)
        {
            ScheduledScraperConfiguration? configuration = await dbContext.ScheduledScraperConfigurations
                                                                          .SingleOrDefaultAsync(
                                                                              entry => entry.Key == PrimaryConfigurationKey,
                                                                              cancellationToken);

            if(configuration is not null)
            {
                return configuration;
            }

            ScheduledScraperConfiguration createdConfiguration = new()
            {
                Key = PrimaryConfigurationKey,
                Enabled = backgroundJobOptions.ScheduledScraper.Enabled,
                ScheduleHour = Math.Clamp(backgroundJobOptions.ScheduledScraper.ScheduleHour, 0, 23),
                ScheduleMinute = Math.Clamp(backgroundJobOptions.ScheduledScraper.ScheduleMinute, 0, 59),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.ScheduledScraperConfigurations.Add(createdConfiguration);
            return createdConfiguration;
        }

        private ScheduledScraperSchedule CreateDefaultSchedule()
        {
            return new ScheduledScraperSchedule(
                backgroundJobOptions.ScheduledScraper.Enabled,
                Math.Clamp(backgroundJobOptions.ScheduledScraper.ScheduleHour, 0, 23),
                Math.Clamp(backgroundJobOptions.ScheduledScraper.ScheduleMinute, 0, 59),
                Math.Max(0, backgroundJobOptions.ScheduledScraper.TimeoutMinutes),
                null);
        }

        private ScheduledScraperSchedule MapSchedule(ScheduledScraperConfiguration configuration)
        {
            return new ScheduledScraperSchedule(
                configuration.Enabled,
                Math.Clamp(configuration.ScheduleHour, 0, 23),
                Math.Clamp(configuration.ScheduleMinute, 0, 59),
                Math.Max(0, backgroundJobOptions.ScheduledScraper.TimeoutMinutes),
                configuration.LastTriggeredAtUtc);
        }
    }
}