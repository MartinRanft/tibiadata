namespace TibiaDataApi.Services.Entities.Scraping
{
    public class ScrapeError
    {
        public int Id { get; set; }

        public int ScrapeLogId { get; set; }

        public ScrapeLog? ScrapeLog { get; set; }

        public string Scope { get; set; } = string.Empty;

        public string? PageTitle { get; set; }

        public string? ItemName { get; set; }

        public string ErrorType { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? DetailsJson { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}