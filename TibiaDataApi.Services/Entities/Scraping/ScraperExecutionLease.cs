namespace TibiaDataApi.Services.Entities.Scraping
{
    public class ScraperExecutionLease
    {
        public required string Name { get; set; }

        public required string OwnerId { get; set; }

        public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}