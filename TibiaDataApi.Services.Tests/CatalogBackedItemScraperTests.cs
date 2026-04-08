using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Scraper.Implementations;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Tests
{
    public sealed class CatalogBackedItemScraperTests
    {
        [Fact]
        public void BuildItem_ParsesKnownWeaponFieldsAndAdditionalAttributes()
        {
            WikiCategory category = new()
            {
                Id = 7,
                Slug = "rods",
                Name = "Rods",
                ContentType = WikiContentType.Item,
                GroupSlug = "weapons",
                GroupName = "Weapons",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Rods",
                ObjectClass = "Weapons",
                SortOrder = 1,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            const string content = """
                                   {{Object
                                   |name=Eldritch Rod
                                   |actualname=eldritch rod
                                   |article=an
                                   |itemid=36674
                                   |implemented=12.70.10953
                                   |levelrequired=250
                                   |vocrequired=druids
                                   |primarytype=Rods
                                   |objectclass=Weapons
                                   |marketable=yes
                                   |walkable=yes
                                   |weight=35.00
                                   |damagetype=Ice
                                   |damagerange=85-105
                                   |range=4
                                   |manacost=22
                                   |imbueslots=2
                                   |upgradeclass=4
                                   |attrib=magic level +2, healing magic level +2
                                   |notes=It has a gilded version.
                                   |slot=Weapon Hand
                                   |lightcolor=71
                                   |lightradius=3
                                   |resist=earth +4%
                                   }}
                                   {{Dropped By|The Brainstealer}}
                                   """;

            TestCatalogBackedItemScraper scraper = new("rods");

            Item item = scraper.Parse("Eldritch Rod", content, category);

            Assert.Equal("Eldritch Rod", item.Name);
            Assert.Equal("eldritch rod", item.NormalizedName);
            Assert.Equal("eldritch rod", item.ActualName);
            Assert.Equal("eldritch rod", item.NormalizedActualName);
            Assert.Equal("an", item.Article);
            Assert.Equal("Rods", item.PrimaryType);
            Assert.Equal("Weapons", item.ObjectClass);
            Assert.Equal("250", item.LevelRequired);
            Assert.Equal("druids", item.Vocation);
            Assert.Equal("Ice", item.DamageType);
            Assert.Equal("85-105", item.DamageRange);
            Assert.Equal("4", item.Range);
            Assert.Equal("35.00", item.Weight);
            Assert.Equal(["36674"], item.ItemId);
            Assert.Equal(["The Brainstealer"], item.DroppedBy);
            Assert.NotNull(item.AdditionalAttributesJson);
            Assert.Contains("Weapon Hand", item.AdditionalAttributesJson);
            Assert.Contains("lightRadius", item.AdditionalAttributesJson);
            Assert.Contains("22", item.AdditionalAttributesJson);
        }

        [Fact]
        public void BuildItem_UsesContainerProfileSpecificAliases()
        {
            WikiCategory category = new()
            {
                Id = 9,
                Slug = "containers",
                Name = "Containers",
                ContentType = WikiContentType.Item,
                GroupSlug = "household-items",
                GroupName = "Household Items",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Containers",
                ObjectClass = "HouseholdItem",
                SortOrder = 1,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            const string content = """
                                   {{Object
                                   |name=Backpack
                                   |actualname=backpack
                                   |article=a
                                   |itemid=2854
                                   |primarytype=Containers
                                   |objectclass=HouseholdItem
                                   |weight=18.00
                                   |capacity=20
                                   |hangable=no
                                   |moveable=yes
                                   |pickupable=yes
                                   }}
                                   """;

            TestCatalogBackedItemScraper scraper = new("containers");

            Item item = scraper.Parse("Backpack", content, category);

            Assert.Equal("Backpack", item.Name);
            Assert.Equal("backpack", item.NormalizedName);
            Assert.Equal("Containers", item.PrimaryType);
            Assert.NotNull(item.AdditionalAttributesJson);
            Assert.Contains("capacity", item.AdditionalAttributesJson);
            Assert.Contains("hangable", item.AdditionalAttributesJson);
            Assert.Contains("moveable", item.AdditionalAttributesJson);
        }

        private sealed class TestCatalogBackedItemScraper(string categorySlug)
        : CatalogBackedItemScraper(
            categorySlug,
            new StubTibiaWikiHttpService(),
            new NullItemImageSyncService(),
            NullLogger.Instance)
        {
            public Item Parse(string title, string content, WikiCategory category)
            {
                return BuildItem(title, content, category);
            }
        }

        private sealed class StubTibiaWikiHttpService : ITibiaWikiHttpService
        {
            public Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(string.Empty);
            }

            public Task<byte[]> GetBytesAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Array.Empty<byte>());
            }
        }

        private sealed class NullItemImageSyncService : IItemImageSyncService
        {
            public Task QueuePrimaryImageSyncAsync(
                int itemId,
                string wikiPageTitle,
                bool forceSync,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<ItemImageSyncBatchResult> SyncPendingAsync(int batchSize, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ItemImageSyncBatchResult(0, 0, 0, 0, 0));
            }
        }
    }
}