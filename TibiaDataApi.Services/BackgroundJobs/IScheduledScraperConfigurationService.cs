namespace TibiaDataApi.Services.BackgroundJobs
{
    public interface IScheduledScraperConfigurationService
    {
        Task<ScheduledScraperSchedule> GetAsync(CancellationToken cancellationToken = default);

        Task<ScheduledScraperSchedule> UpdateAsync(
            bool enabled,
            int scheduleHour,
            int scheduleMinute,
            CancellationToken cancellationToken = default);

        Task<bool> ShouldRunAtAsync(DateTime localNow, CancellationToken cancellationToken = default);

        Task MarkTriggeredAsync(DateTime triggeredAtUtc, CancellationToken cancellationToken = default);
    }

    public sealed record ScheduledScraperSchedule(
        bool Enabled,
        int ScheduleHour,
        int ScheduleMinute,
        int TimeoutMinutes,
        DateTime? LastTriggeredAtUtc);
}