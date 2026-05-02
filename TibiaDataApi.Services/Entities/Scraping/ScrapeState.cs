namespace TibiaDataApi.Services.Entities.Scraping
{
    public static class ScrapeState
    {
        public const string Pending = "Pending";
        public const string Running = "Running";
        public const string CancellationRequested = "CancellationRequested";
        public const string Cancelled = "Cancelled";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}