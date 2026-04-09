using TibiaDataApi.Services.Categories;

namespace TibiaDataApi.Services.Entities.Content
{
    public class WikiArticle
    {
        public int Id { get; set; }

        public WikiContentType ContentType { get; set; }

        public required string Title { get; set; }

        public required string NormalizedTitle { get; set; }

        public string? Summary { get; set; }

        public string? PlainTextContent { get; set; }

        public string? RawWikiText { get; set; }

        public string? InfoboxTemplate { get; set; }

        public string? InfoboxJson { get; set; }

        public List<string> Sections { get; set; } = [];

        public List<string> LinkedTitles { get; set; } = [];

        public string? AdditionalAttributesJson { get; set; }

        public string? WikiUrl { get; set; }

        public DateTime? LastSeenAt { get; set; }

        public bool IsMissingFromSource { get; set; }

        public DateTime? MissingSince { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public List<WikiArticleCategory> WikiArticleCategories { get; set; } = [];
    }
}