using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Entities.Items;

namespace TibiaDataApi.Services.Entities.Categories
{
    public class WikiCategory
    {
        public int Id { get; set; }

        public required string Slug { get; set; }

        public required string Name { get; set; }

        public WikiContentType ContentType { get; set; } = WikiContentType.Item;

        public required string GroupSlug { get; set; }

        public required string GroupName { get; set; }

        public WikiCategorySourceKind SourceKind { get; set; } = WikiCategorySourceKind.CategoryMembers;

        public required string SourceTitle { get; set; }

        public string? SourceSection { get; set; }

        public string? ObjectClass { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<Item> Items { get; set; } = new();

        public List<WikiArticleCategory> WikiArticleCategories { get; set; } = new();
    }
}