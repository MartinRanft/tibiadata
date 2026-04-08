namespace TibiaDataApi.Services.Entities.Monitoring
{
    public sealed class ScheduledScraperConfiguration
    {
        public string Key { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        public int ScheduleHour { get; set; } = 3;

        public int ScheduleMinute { get; set; } = 0;

        public DateTime? LastTriggeredAtUtc { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}