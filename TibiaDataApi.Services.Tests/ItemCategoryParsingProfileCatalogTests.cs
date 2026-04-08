using TibiaDataApi.Services.Scraper.Parsing;

namespace TibiaDataApi.Services.Tests
{
    public sealed class ItemCategoryParsingProfileCatalogTests
    {
        [Fact]
        public void AllItemCategoriesHaveDedicatedParsingProfiles()
        {
            IReadOnlyList<string> missingCategorySlugs = ItemCategoryParsingProfileCatalog.GetMissingItemCategorySlugs();

            Assert.Empty(missingCategorySlugs);
        }
    }
}