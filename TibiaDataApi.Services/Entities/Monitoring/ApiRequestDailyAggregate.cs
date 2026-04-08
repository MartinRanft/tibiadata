namespace TibiaDataApi.Services.Entities.Monitoring
{
    public class ApiRequestDailyAggregate
    {
        public int Id { get; set; }

        public DateTime DayUtc { get; set; }

        public int RequestCount { get; set; }

        public int ErrorCount { get; set; }

        public int BlockedCount { get; set; }

        public int CacheHitCount { get; set; }

        public int CacheMissCount { get; set; }

        public int CacheBypassCount { get; set; }

        public long TotalResponseSizeBytes { get; set; }

        public double TotalDurationMs { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
