using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.DataBaseService.Creatures;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class CreaturesDataBaseServiceTests
    {
        [Fact]
        public async Task GetCreatureDetailsByIdAsync_ReturnsStructuredInfoboxData()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            dbContext.Creatures.Add(new Creature
            {
                Name = "Dragon",
                NormalizedName = "dragon",
                Hitpoints = 1000,
                Experience = 700,
                InfoboxJson =
                    """
                    {"name":"Dragon","actualname":"dragon","armor":"22","mitigation":"1.05","creatureclass":"Reptile","strategy":"Use fire protection.","location":"Dragon Lairs.","physicaldmgmod":"100%","history":"One of the oldest monsters."}
                    """,
                BestiaryJson =
                    """
                    {"classSlug":"dragon","difficultySlug":"medium","occurrence":"common"}
                    """,
                LastUpdated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            CreaturesDataBaseService service = CreateService(dbContext);

            Contracts.Public.Creatures.CreatureDetailsResponse? result = await service.GetCreatureDetailsByIdAsync(1);

            Assert.NotNull(result);
            Assert.NotNull(result!.StructuredData);
            Assert.NotNull(result.StructuredData!.Infobox);
            Assert.Equal("22", result.StructuredData.Infobox!.Armor);
            Assert.Equal("1.05", result.StructuredData.Infobox.Mitigation);
            Assert.Equal("Reptile", result.StructuredData.Infobox.CreatureClass);
            Assert.Equal("Use fire protection.", result.StructuredData.Infobox.Strategy);
            Assert.Equal("Dragon Lairs.", result.StructuredData.Infobox.Location);
            Assert.Equal("dragon", result.StructuredData.Infobox.BestiaryClass);
            Assert.Equal("medium", result.StructuredData.Infobox.BestiaryDifficulty);
            Assert.Equal("common", result.StructuredData.Infobox.BestiaryOccurrence);
            Assert.Equal("100%", result.StructuredData.Infobox.PhysicalDamageModifier);
            Assert.Equal("One of the oldest monsters.", result.StructuredData.Infobox.History);
            Assert.True(result.StructuredData.Infobox.Fields?.ContainsKey("strategy"));
        }

        private static TibiaDbContext CreateDbContext()
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                                                       .Options;

            return new TibiaDbContext(options);
        }

        private static CreaturesDataBaseService CreateService(TibiaDbContext dbContext)
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddHybridCache();
            services.AddSingleton(new CachingOptions());

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            return ActivatorUtilities.CreateInstance<CreaturesDataBaseService>(serviceProvider, dbContext);
        }
    }
}
