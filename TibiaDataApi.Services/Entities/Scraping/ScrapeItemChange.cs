namespace TibiaDataApi.Services.Entities.Scraping
{
    public class ScrapeItemChange
    {
        public int Id { get; set; }

        public int ScrapeLogId { get; set; }

        public ScrapeLog? ScrapeLog { get; set; }

        public int? ItemId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string ChangeType { get; set; } = string.Empty;

        public string? CategorySlug { get; set; }

        public string? CategoryName { get; set; }

        public string? ChangedFieldsJson { get; set; }

        public string? BeforeJson { get; set; }

        public string? AfterJson { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}