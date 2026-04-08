using TibiaDataApi.Services.Entities.Assets;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Scraper;

namespace TibiaDataApi.Services.Tests
{
    public sealed class ItemChangeDetectorTests
    {
        [Fact]
        public void GetChangedFields_IgnoresItemAssetNavigationChanges()
        {
            Item existing = CreateItem();
            existing.ItemAssets.Add(new ItemAsset
            {
                Id = 1,
                ItemId = 10,
                AssetId = 20,
                AssetKind = AssetKind.PrimaryImage
            });

            Item incoming = CreateItem();
            incoming.ItemAssets.Add(new ItemAsset
            {
                Id = 2,
                ItemId = 10,
                AssetId = 21,
                AssetKind = AssetKind.PrimaryImage
            });

            IReadOnlyList<string> changedFields = ItemChangeDetector.GetChangedFields(existing, incoming);

            Assert.Empty(changedFields);
        }

        private static Item CreateItem()
        {
            return new Item
            {
                Id = 10,
                Name = "Eldritch Rod",
                NormalizedName = "eldritch rod",
                TemplateType = "Object",
                WikiUrl = "https://tibia.fandom.com/wiki/Eldritch_Rod",
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}