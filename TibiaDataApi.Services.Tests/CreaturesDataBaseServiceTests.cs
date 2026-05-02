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
                    {"name":"Dragon","actualname":"dragon","armor":"22","mitigation":"1.05","creatureclass":"Reptile","strategy":"Use fire protection.","location":"Dragon Lairs.","physicaldmgmod":"100%","firedmgmod":"85%","healmod":"110%","maxdmg":"250","speed":"220","runsat":"100","usespells":"yes","pushable":"no","pushobjects":"yes","walksaround":"yes","isboss":"no","history":"One of the oldest monsters."}
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
            Assert.NotNull(result.StructuredData.ResistanceSummary);
            Assert.Equal(100, result.StructuredData.ResistanceSummary!.PhysicalPercent);
            Assert.Equal(85, result.StructuredData.ResistanceSummary.FirePercent);
            Assert.Equal(110, result.StructuredData.ResistanceSummary.HealingPercent);
            Assert.NotNull(result.StructuredData.CombatProperties);
            Assert.Equal(22, result.StructuredData.CombatProperties!.Armor);
            Assert.Equal(1.05m, result.StructuredData.CombatProperties.Mitigation);
            Assert.Equal(250, result.StructuredData.CombatProperties.MaxDamage);
            Assert.Equal(220, result.StructuredData.CombatProperties.Speed);
            Assert.Equal(100, result.StructuredData.CombatProperties.RunsAt);
            Assert.True(result.StructuredData.CombatProperties.UsesSpells);
            Assert.False(result.StructuredData.CombatProperties.Pushable);
            Assert.True(result.StructuredData.CombatProperties.PushObjects);
            Assert.True(result.StructuredData.CombatProperties.WalksAround);
            Assert.False(result.StructuredData.CombatProperties.IsBoss);
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
