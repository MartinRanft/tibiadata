namespace TibiaDataApi.Services.Scraper.Runtime
{
    public sealed class ScraperRuntimeOptions
    {
        public const string SectionName = "ScraperRuntime";

        public string ExecutionLeaseName { get; set; } = "scraper-runtime";

        public int RunTimeoutMinutes { get; set; } = 180;

        public int ExecutionLeaseDurationMinutes { get; set; } = 5;

        public int ExecutionLeaseRenewalSeconds { get; set; } = 30;

        public int MaxLeaseRenewalFailures { get; set; } = 3;

        public int MaxConcurrentScrapers { get; set; } = 0;
    }
}