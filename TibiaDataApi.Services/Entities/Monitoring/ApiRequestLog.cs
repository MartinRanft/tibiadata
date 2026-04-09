namespace TibiaDataApi.Services.Entities.Monitoring
{
    public class ApiRequestLog
    {
        public int Id { get; set; }

        public string IpAddress { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public string Route { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public double DurationMs { get; set; }

        public string? UserAgent { get; set; }

        public long ResponseSizeBytes { get; set; }

        public string CacheStatus { get; set; } = ApiCacheStatus.Bypass;

        public bool IsBlocked { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    public static class ApiCacheStatus
    {
        public const string Hit = "Hit";
        public const string Miss = "Miss";
        public const string Bypass = "Bypass";
    }
}
