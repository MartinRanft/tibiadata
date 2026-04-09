using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TibiaDataApi.Contracts.Public.Bestiary;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.DataBaseService.Bestiary;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class BestiaryDataBaseServiceTests
    {
        [Fact]
        public async Task GetBestiaryClassesAsync_ReturnsCreatureCounts()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            dbContext.Creatures.AddRange(
                CreateCreature("Acid Blob", "slime", "medium", "common"),
                CreateCreature("Abyssal Calamary", "aquatic", "easy", "common"),
                CreateCreature("Ghoul", "undead", "easy", "rare"));

            await dbContext.SaveChangesAsync();

            BestiaryDataBaseService service = CreateService(dbContext);

            IReadOnlyList<BestiaryClassResponse> classes = await service.GetBestiaryClassesAsync();

            Assert.Equal(1, classes.Single(entry => entry.Slug == "slime").CreatureCount);
            Assert.Equal(1, classes.Single(entry => entry.Slug == "aquatic").CreatureCount);
            Assert.Equal(1, classes.Single(entry => entry.Slug == "undead").CreatureCount);
            Assert.Equal(0, classes.Single(entry => entry.Slug == "demon").CreatureCount);
        }

        [Fact]
        public async Task GetFilteredBestiaryCreaturesAsync_FiltersAndSortsByCharmPoints()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            dbContext.Creatures.AddRange(
                CreateCreature("Ghoul", "undead", "easy", "common"),
                CreateCreature("Demon Skeleton", "undead", "medium", "rare"),
                CreateCreature("Dragon", "dragon", "hard", "common"));

            await dbContext.SaveChangesAsync();

            BestiaryDataBaseService service = CreateService(dbContext);

            BestiaryFilteredCreaturesResponse result = await service.GetFilteredBestiaryCreaturesAsync(
                classSlug: "undead",
                sortBy: "charmPoints",
                descending: true,
                page: 1,
                pageSize: 10);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal("undead", result.BestiaryClass);
            Assert.Equal("charm-points", result.Sort);
            Assert.Equal(["Demon Skeleton", "Ghoul"], result.Creatures.Select(entry => entry.CreatureName).ToArray());
            Assert.Equal([50, 15], result.Creatures.Select(entry => entry.CharmPoints).ToArray());
        }

        [Fact]
        public async Task GetFilteredBestiaryCreaturesAsync_IgnoresMalformedBestiaryEntries()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            dbContext.Creatures.AddRange(
                new Creature
                {
                    Name = "Broken Entry",
                    NormalizedName = "broken entry",
                    LastUpdated = DateTime.UtcNow,
                    BestiaryJson = """{"classSlug":"|-bestiarylevel--=","difficultySlug":"medium","occurrence":"common"}"""
                },
                CreateCreature("Ghoul", "undead", "easy", "common"));

            await dbContext.SaveChangesAsync();

            BestiaryDataBaseService service = CreateService(dbContext);

            BestiaryFilteredCreaturesResponse result = await service.GetFilteredBestiaryCreaturesAsync();

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(["Ghoul"], result.Creatures.Select(entry => entry.CreatureName).ToArray());
        }

        private static TibiaDbContext CreateDbContext()
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                                                       .Options;

            return new TibiaDbContext(options);
        }

        private static BestiaryDataBaseService CreateService(TibiaDbContext dbContext)
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddHybridCache();
            services.AddSingleton(new CachingOptions());

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            return ActivatorUtilities.CreateInstance<BestiaryDataBaseService>(serviceProvider, dbContext);
        }

        private static Creature CreateCreature(string name, string classSlug, string difficultySlug, string occurrence)
        {
            return new Creature
            {
                Name = name,
                NormalizedName = name.ToLowerInvariant(),
                LastUpdated = DateTime.UtcNow,
                BestiaryJson =
                    $$"""
                      {"classSlug":"{{classSlug}}","categorySlug":"{{classSlug}}","difficultySlug":"{{difficultySlug}}","occurrence":"{{occurrence}}"}
                      """
            };
        }
    }
}
