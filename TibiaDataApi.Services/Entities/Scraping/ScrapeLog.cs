namespace TibiaDataApi.Services.Entities.Scraping
{
    public class ScrapeLog
    {
        public int Id { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public bool Success { get; set; }

        public string Status { get; set; } = "Pending";

        public string? TriggeredBy { get; set; }

        public string? ScraperName { get; set; }

        public string? CategoryName { get; set; }

        public string? CategorySlug { get; set; }

        public string? ErrorMessage { get; set; }

        public string? ErrorType { get; set; }

        
        public int ItemsProcessed { get; set; }

        public int ItemsAdded { get; set; }

        public int ItemsUpdated { get; set; }

        public int ItemsUnchanged { get; set; }

        public int ItemsFailed { get; set; }

        public int ItemsMissingFromSource { get; set; }

        public int PagesDiscovered { get; set; }

        public int PagesProcessed { get; set; }

        public int PagesFailed { get; set; }

        
        public string? ChangesJson { get; set; }

        public string? MetadataJson { get; set; }

        public List<ScrapeItemChange> ItemChanges { get; set; } = new();

        public List<ScrapeError> Errors { get; set; } = new();
    }
}