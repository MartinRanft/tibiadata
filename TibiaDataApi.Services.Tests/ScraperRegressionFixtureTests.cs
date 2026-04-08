using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Scraper.Implementations;
using TibiaDataApi.Services.Scraper.Parsing;
using TibiaDataApi.Services.Text;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Tests
{
    public sealed class ScraperRegressionFixtureTests
    {
        [Theory]
        [MemberData(nameof(GetItemCategorySlugs))]
        public void BuildItem_ParsesRegressionFixture_ForEveryItemCategory(string categorySlug)
        {
            WikiCategoryDefinition definition = TibiaWikiCategoryCatalog.GetRequiredDefinition(WikiContentType.Item, categorySlug);
            ItemCategoryParsingProfile profile = ItemCategoryParsingProfileCatalog.GetRequiredProfile(categorySlug);
            string content = LoadFixture("items", $"{profile.Key}.wiki");
            ItemRegressionExpectation expectation = GetItemExpectation(profile.Key);

            TestCatalogBackedItemScraper scraper = new(categorySlug);
            Item item = scraper.Parse(expectation.Title, content, CreateCategory(definition));

            Assert.Equal(expectation.ExpectedName, item.Name);
            Assert.Equal(EntityNameNormalizer.Normalize(expectation.ExpectedName), item.NormalizedName);
            Assert.Equal(definition.Name, item.PrimaryType);
            Assert.Equal(definition.ObjectClass, item.ObjectClass);
            Assert.NotNull(item.AdditionalAttributesJson);
            Assert.Equal(["1001"], item.ItemId);

            expectation.Assert(item);
        }

        [Fact]
        public void BuildCreature_ParsesRegressionFixture()
        {
            string content = LoadFixture("creatures", "Scarab.wiki");
            TestCatalogBackedCreatureScraper scraper = new("creatures");

            Creature creature = scraper.Parse("Scarab", content);

            Assert.Equal("Scarab", creature.Name);
            Assert.Equal("scarab", creature.NormalizedName);
            Assert.Equal(320, creature.Hitpoints);
            Assert.Equal(120, creature.Experience);
            Assert.Contains("Gold Coin", creature.LootStatisticsJson);
            Assert.Contains("Piece of Scarab Shell", creature.LootStatisticsJson);
        }

        [Fact]
        public void BuildWikiArticle_ParsesRegressionFixture()
        {
            string rawWikiText = LoadFixture("wiki-articles", "Quest.raw.wiki");
            string renderedHtml = LoadFixture("wiki-articles", "Quest.rendered.html");
            TestCatalogBackedWikiArticleScraper scraper = new(WikiContentType.Quest, "quest-overview-pages");

            WikiArticle article = scraper.Parse("The Desert Dungeon Quest", rawWikiText, renderedHtml);

            Assert.Equal(WikiContentType.Quest, article.ContentType);
            Assert.Equal("The Desert Dungeon Quest", article.Title);
            Assert.Equal("the desert dungeon quest", article.NormalizedTitle);
            Assert.Equal("The Desert Dungeon Quest grants access to several dungeons.", article.Summary);
            Assert.Contains("Requirements", article.Sections);
            Assert.Contains("Walkthrough", article.Sections);
            Assert.Contains("Alesar", article.LinkedTitles);
            Assert.Contains("Darashia", article.LinkedTitles);
            Assert.Equal("Quest", article.InfoboxTemplate);
            Assert.NotNull(article.InfoboxJson);
            Assert.Contains("reward", article.InfoboxJson);
            Assert.Contains("location", article.InfoboxJson);
        }

        public static IEnumerable<object[]> GetItemCategorySlugs()
        {
            return TibiaWikiCategoryCatalog.All
                                          .Where(entry => entry.ContentType == WikiContentType.Item)
                                          .OrderBy(entry => entry.Slug, StringComparer.OrdinalIgnoreCase)
                                          .Select(entry => new object[] { entry.Slug });
        }

        private static WikiCategory CreateCategory(WikiCategoryDefinition definition)
        {
            return new WikiCategory
            {
                Id = 1,
                Slug = definition.Slug,
                Name = definition.Name,
                ContentType = definition.ContentType,
                GroupSlug = definition.GroupSlug,
                GroupName = definition.GroupName,
                SourceKind = definition.SourceKind,
                SourceTitle = definition.SourceTitle,
                ObjectClass = definition.ObjectClass,
                SortOrder = definition.SortOrder,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static ItemRegressionExpectation GetItemExpectation(string profileKey)
        {
            return profileKey switch
            {
                "BodyEquipment" => new ItemRegressionExpectation(
                    "Guardian Plate",
                    "Guardian Plate",
                    item =>
                    {
                        Assert.Equal("guardian plate", item.ActualName);
                        Assert.Equal("a", item.Article);
                        Assert.Equal("15", item.Armor);
                        Assert.Contains("\"slotPosition\":\"body\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"resistAll\":\"4%\"", item.AdditionalAttributesJson);
                    }),
                "MeleeWeapons" => new ItemRegressionExpectation(
                    "Battle Axe",
                    "Battle Axe",
                    item =>
                    {
                        Assert.Equal("52", item.Attack);
                        Assert.Equal("31", item.Defense);
                        Assert.Equal("2", item.DefenseMod);
                        Assert.Contains("\"twoHanded\":\"yes\"", item.AdditionalAttributesJson);
                    }),
                "MagicWeapons" => new ItemRegressionExpectation(
                    "Arcane Wand",
                    "Arcane Wand",
                    item =>
                    {
                        Assert.Equal("38", item.Attack);
                        Assert.Equal("Energy", item.DamageType);
                        Assert.Contains("\"manaCost\":\"18\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"cooldown\":\"2s\"", item.AdditionalAttributesJson);
                    }),
                "DistanceWeapons" => new ItemRegressionExpectation(
                    "Hunter Bow",
                    "Hunter Bow",
                    item =>
                    {
                        Assert.Equal("30", item.Attack);
                        Assert.Equal("6", item.Range);
                        Assert.Contains("\"ammoType\":\"arrow\"", item.AdditionalAttributesJson);
                    }),
                "Ammunition" => new ItemRegressionExpectation(
                    "Crystalline Arrow",
                    "Crystalline Arrow",
                    item =>
                    {
                        Assert.Equal("52", item.Attack);
                        Assert.Equal("Ice", item.DamageType);
                        Assert.Contains("\"requiredWeapon\":\"bow\"", item.AdditionalAttributesJson);
                    }),
                "BooksAndDocuments" => new ItemRegressionExpectation(
                    "Explorer's Notes",
                    "Explorer's Notes",
                    item =>
                    {
                        Assert.Contains("\"text\":\"The path north is dangerous.\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"author\":\"Archivist\"", item.AdditionalAttributesJson);
                    }),
                "Containers" => new ItemRegressionExpectation(
                    "Traveller Backpack",
                    "Traveller Backpack",
                    item =>
                    {
                        Assert.Contains("\"capacity\":\"20\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"hangable\":\"no\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"moveable\":\"yes\"", item.AdditionalAttributesJson);
                    }),
                "Decorations" => new ItemRegressionExpectation(
                    "Festival Banner",
                    "Festival Banner",
                    item =>
                    {
                        Assert.Contains("\"houseUse\":\"display\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"eventSource\":\"summer court\"", item.AdditionalAttributesJson);
                    }),
                "Consumables" => new ItemRegressionExpectation(
                    "Blessed Cake",
                    "Blessed Cake",
                    item =>
                    {
                        Assert.Equal("150", item.Value);
                        Assert.Contains("\"nutrition\":\"9\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"healAmount\":\"250\"", item.AdditionalAttributesJson);
                    }),
                "Accessories" => new ItemRegressionExpectation(
                    "Arcane Necklace",
                    "Arcane Necklace",
                    item =>
                    {
                        Assert.Equal("80", item.LevelRequired);
                        Assert.Equal("sorcerers,druids", item.Vocation);
                        Assert.Contains("\"duration\":\"60 minutes\"", item.AdditionalAttributesJson);
                    }),
                "Keys" => new ItemRegressionExpectation(
                    "Key 4501",
                    "Key 4501",
                    item =>
                    {
                        Assert.Contains("\"keyNumber\":\"4501\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"doorLevel\":\"2\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"usage\":\"Kazordoon vault\"", item.AdditionalAttributesJson);
                    }),
                "LightSources" => new ItemRegressionExpectation(
                    "Sun Lantern",
                    "Sun Lantern",
                    item =>
                    {
                        Assert.Contains("\"lightRadius\":\"6\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"lightColor\":\"215\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"duration\":\"30 minutes\"", item.AdditionalAttributesJson);
                    }),
                "UtilityItems" => new ItemRegressionExpectation(
                    "Painter's Brush",
                    "Painter's Brush",
                    item =>
                    {
                        Assert.Contains("\"toolType\":\"painting\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"teleportTo\":\"gallery hall\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"paintColor\":\"crimson\"", item.AdditionalAttributesJson);
                    }),
                "MagicalMisc" => new ItemRegressionExpectation(
                    "Runic Sigil",
                    "Runic Sigil",
                    item =>
                    {
                        Assert.Equal("Holy", item.DamageType);
                        Assert.Equal("120", item.LevelRequired);
                        Assert.Contains("\"spellName\":\"Divine Missile\"", item.AdditionalAttributesJson);
                    }),
                "Valuables" => new ItemRegressionExpectation(
                    "Jeweled Cup",
                    "Jeweled Cup",
                    item =>
                    {
                        Assert.Contains("\"material\":\"gold\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"stackSize\":\"5\"", item.AdditionalAttributesJson);
                    }),
                "GenericMisc" => new ItemRegressionExpectation(
                    "Old Boot",
                    "Old Boot",
                    item =>
                    {
                        Assert.Contains("\"moveable\":\"yes\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"usage\":\"collector curiosity\"", item.AdditionalAttributesJson);
                        Assert.Contains("\"flavorText\":\"Still smells like swamp.\"", item.AdditionalAttributesJson);
                    }),
                _ => throw new InvalidOperationException($"No regression expectation is defined for item parsing profile '{profileKey}'.")
            };
        }

        private static string LoadFixture(string category, string fileName)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "TestData", "ScraperRegression", category, fileName);

            if(!File.Exists(path))
            {
                throw new FileNotFoundException($"Missing scraper regression fixture '{path}'.", path);
            }

            return File.ReadAllText(path);
        }

        private sealed record ItemRegressionExpectation(
            string Title,
            string ExpectedName,
            Action<Item> Assert);

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

        private sealed class TestCatalogBackedCreatureScraper(string categorySlug)
        : CatalogBackedCreatureScraper(
            categorySlug,
            new StubTibiaWikiHttpService(),
            new NullCreatureImageSyncService(),
            NullLogger.Instance)
        {
            public Creature Parse(string title, string content)
            {
                return BuildCreature(title, content);
            }
        }

        private sealed class TestCatalogBackedWikiArticleScraper(WikiContentType contentType, string categorySlug)
        : CatalogBackedWikiArticleScraper(
            contentType,
            categorySlug,
            new StubTibiaWikiHttpService(),
            NullLogger.Instance)
        {
            public WikiArticle Parse(string title, string rawWikiText, string renderedHtml)
            {
                return BuildArticle(title, rawWikiText, renderedHtml);
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

        private sealed class NullCreatureImageSyncService : ICreatureImageSyncService
        {
            public Task QueuePrimaryImageSyncAsync(
                int creatureId,
                string wikiPageTitle,
                bool forceSync,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<CreatureImageSyncBatchResult> SyncPendingAsync(int batchSize, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CreatureImageSyncBatchResult(0, 0, 0, 0, 0));
            }
        }
    }
}
