using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper.Implementations;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Tests
{
    public sealed class CatalogBackedCreatureScraperTests
    {
        [Fact]
        public void BuildCreature_ParsesHitpointsExperienceLootAndBestiary()
        {
            TestCatalogBackedCreatureScraper scraper = new("creatures");

            Creature creature = scraper.Parse("Scarab",
                """
                {{Infobox Creature
                | name = Scarab
                | hp = 320
                | exp = 120
                | bestiaryclass = Vermin
                | bestiarylevel = Medium
                | occurrence = Common
                | loot = {{Loot Table
                 |{{Loot Item|0-52|Gold Coin|common}}
                 |{{Loot Item|Piece of Scarab Shell|semi-rare}}
                }}
                }}
                """);

            Assert.Equal("Scarab", creature.Name);
            Assert.Equal("scarab", creature.NormalizedName);
            Assert.Equal(320, creature.Hitpoints);
            Assert.Equal(120, creature.Experience);
            Assert.NotNull(creature.BestiaryJson);
            Assert.Contains("\"classSlug\":\"vermin\"", creature.BestiaryJson);
            Assert.Contains("\"difficultySlug\":\"medium\"", creature.BestiaryJson);
            Assert.Contains("Gold Coin", creature.LootStatisticsJson);
            Assert.Contains("Piece of Scarab Shell", creature.LootStatisticsJson);
        }

        [Fact]
        public void BuildCreature_ParsesBosstiaryCategory_ForBossCreatures()
        {
            TestCatalogBackedCreatureScraper scraper = new("creatures");

            Creature creature = scraper.Parse("Grand Master Oberon",
                """
                {{Infobox Creature
                | name = Grand Master Oberon
                | hp = 30000
                | exp = 20000
                | isboss = yes
                | bosstiaryclass = Archfoe
                | cooldown = 20
                | bestiaryname = grandmasteroberon
                }}
                """);

            Assert.Equal("Grand Master Oberon", creature.Name);
            Assert.NotNull(creature.BestiaryJson);
            Assert.Contains("\"bosstiaryCategorySlug\":\"archfoe\"", creature.BestiaryJson);
            Assert.Contains("\"bosstiaryCategory\":\"Archfoe\"", creature.BestiaryJson);
        }

        [Fact]
        public void BuildCreature_DoesNotShiftEmptyScalarFieldsIntoNextParameter()
        {
            TestCatalogBackedCreatureScraper scraper = new("creatures");

            Creature creature = scraper.Parse("Dragon",
                """
                {{Infobox Creature
                | name = Dragon
                | hp = 1000
                | exp =
                | bestiaryclass = Dragon
                | bestiarylevel = Medium
                | occurrence = Common
                }}
                """);

            Assert.Equal("Dragon", creature.Name);
            Assert.Equal(1000, creature.Hitpoints);
            Assert.Equal(0, creature.Experience);
            Assert.NotNull(creature.BestiaryJson);
            Assert.Contains("\"classSlug\":\"dragon\"", creature.BestiaryJson);
            Assert.Contains("\"difficultySlug\":\"medium\"", creature.BestiaryJson);
        }

        [Fact]
        public void BuildCreature_StoresStructuredInfoboxFields()
        {
            TestCatalogBackedCreatureScraper scraper = new("creatures");

            Creature creature = scraper.Parse("Dragon",
                """
                {{Infobox Creature
                | name = Dragon
                | actualname = dragon
                | plural = dragons
                | article = a
                | hp = 1000
                | exp = 700
                | armor = 22
                | mitigation = 1.05
                | summon = 1500
                | convince = 2000
                | illusionable = yes
                | creatureclass = Reptile
                | primarytype = Fire
                | secondarytype = Physical
                | abilities = [[Melee]] + [[Fire Wave]]
                | maxdmg = 250
                | pushable = no
                | pushobjects = yes
                | walksaround = fire, poison
                | walksthrough = fire
                | physicalDmgMod = 100%
                | fireDmgMod = 0%
                | iceDmgMod = 110%
                | healMod = 100%
                | sounds = FCHHHH
                | implemented = 7.4
                | race_id = 34
                | notes = Breathes fire.
                | behaviour = Prefers melee combat.
                | runsat = 150
                | speed = 220
                | strategy = Use fire protection.
                | location = Dragon Lairs.
                | history = One of the oldest monsters.
                | usespells = yes
                | attacktype = Melee
                | spawntype = Regular
                }}
                """);

            Assert.NotNull(creature.InfoboxJson);

            Dictionary<string, string> infobox = JsonSerializer.Deserialize<Dictionary<string, string>>(creature.InfoboxJson!)!;

            Assert.Equal("Dragon", infobox["name"]);
            Assert.Equal("dragon", infobox["actualname"]);
            Assert.Equal("22", infobox["armor"]);
            Assert.Equal("1.05", infobox["mitigation"]);
            Assert.Equal("Reptile", infobox["creatureclass"]);
            Assert.Equal("100%", infobox["physicaldmgmod"]);
            Assert.Equal("Use fire protection.", infobox["strategy"]);
            Assert.Equal("Dragon Lairs.", infobox["location"]);
        }

        [Fact]
        public async Task ExecuteAsync_SkipsMetaPagesAndStoresCreatures()
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                                                       .Options;

            await using TibiaDbContext dbContext = new(options);

            ScrapeLog scrapeLog = new()
            {
                StartedAt = DateTime.UtcNow,
                Status = "Running",
                TriggeredBy = "Test"
            };

            dbContext.ScrapeLogs.Add(scrapeLog);
            await dbContext.SaveChangesAsync();

            CatalogBackedCreatureScraper scraper = new(
                "creatures",
                new StubTibiaWikiHttpService(),
                new NullCreatureImageSyncService(),
                NullLogger.Instance);

            await scraper.ExecuteAsync(dbContext, scrapeLog);

            Creature creature = await dbContext.Creatures.SingleAsync();

            Assert.Equal("Scarab", creature.Name);
            Assert.Equal("scarab", creature.NormalizedName);
            Assert.Equal(320, creature.Hitpoints);
            Assert.Equal(120, creature.Experience);
            Assert.Equal(2, scrapeLog.PagesDiscovered);
            Assert.Equal(1, scrapeLog.PagesProcessed);
            Assert.Equal(1, scrapeLog.ItemsAdded);
            Assert.Contains("NonCreaturePagesSkipped", scrapeLog.MetadataJson);
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

            public Task<CreatureImageSyncBatchResult> SyncPendingAsync(
                int batchSize,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CreatureImageSyncBatchResult(0, 0, 0, 0, 0));
            }
        }

        private sealed class StubTibiaWikiHttpService : ITibiaWikiHttpService
        {
            public Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                if(requestUri.Contains("action=query&list=categorymembers", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult("""
                                           {"query":{"categorymembers":[
                                             {"title":"Scarab"},
                                             {"title":"List of Creatures (Ordered)"}
                                           ]}}
                                           """);
                }

                if(requestUri.Contains("titles=Scarab", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(
                        """{"query":{"pages":{"1486":{"revisions":[{"*":"{{Infobox Creature\n| name = Scarab\n| hp = 320\n| exp = 120\n| loot = {{Loot Table\n |{{Loot Item|0-52|Gold Coin|common}}\n |{{Loot Item|Piece of Scarab Shell|semi-rare}}\n}}\n}}"}]}}}}""");
                }

                if(requestUri.Contains("titles=List%20of%20Creatures%20%28Ordered%29", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult("""{"query":{"pages":{"64179":{"revisions":[{"*":"{{DPL Table|categories = Creatures}}"}]}}}}""");
                }

                return Task.FromResult("""{"query":{"pages":{}}}""");
            }

            public Task<byte[]> GetBytesAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Array.Empty<byte>());
            }
        }
    }
}
