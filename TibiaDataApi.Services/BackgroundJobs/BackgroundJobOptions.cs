namespace TibiaDataApi.Services.BackgroundJobs
{
    public sealed class BackgroundJobOptions
    {
        public const string SectionName = "BackgroundJobs";

        public ScheduledScraperJobOptions ScheduledScraper { get; set; } = new();

        public ItemImageSyncBackgroundJobOptions ItemImageSync { get; set; } = new();

        public CreatureImageSyncBackgroundJobOptions CreatureImageSync { get; set; } = new();
    }

    public sealed class ScheduledScraperJobOptions
    {
        public bool Enabled { get; set; } = true;

        public int ScheduleHour { get; set; } = 3;

        public int ScheduleMinute { get; set; } = 0;

        public int TimeoutMinutes { get; set; } = 0;
    }

    public sealed class ItemImageSyncBackgroundJobOptions
    {
        public bool Enabled { get; set; } = true;

        public int IntervalMinutes { get; set; } = 10;

        public int BatchSize { get; set; } = 25;

        public int MaxParallelWorkers { get; set; } = 0;

        public string LeaseName { get; set; } = "background-job:item-image-sync";

        public int TimeoutMinutes { get; set; } = 20;

        public int LeaseDurationMinutes { get; set; } = 5;

        public int LeaseRenewalSeconds { get; set; } = 30;

        public int MaxLeaseRenewalFailures { get; set; } = 3;
    }

    public sealed class CreatureImageSyncBackgroundJobOptions
    {
        public bool Enabled { get; set; } = true;

        public int IntervalMinutes { get; set; } = 10;

        public int BatchSize { get; set; } = 25;

        public int MaxParallelWorkers { get; set; } = 0;

        public string LeaseName { get; set; } = "background-job:creature-image-sync";

        public int TimeoutMinutes { get; set; } = 20;

        public int LeaseDurationMinutes { get; set; } = 5;

        public int LeaseRenewalSeconds { get; set; } = 30;

        public int MaxLeaseRenewalFailures { get; set; } = 3;
    }
}