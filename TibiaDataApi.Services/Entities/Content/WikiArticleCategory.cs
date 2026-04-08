using TibiaDataApi.Services.Entities.Categories;

namespace TibiaDataApi.Services.Entities.Content
{
    public class WikiArticleCategory
    {
        public int WikiArticleId { get; set; }

        public WikiArticle? WikiArticle { get; set; }

        public int WikiCategoryId { get; set; }

        public WikiCategory? WikiCategory { get; set; }

        public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastSeenAt { get; set; }

        public bool IsMissingFromSource { get; set; }

        public DateTime? MissingSince { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}