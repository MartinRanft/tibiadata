using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TibiaDataApi.Contracts.Public.Bosstiary;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.DataBaseService.Bosstiary;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class BosstiaryDataBaseServiceTests
    {
        [Fact]
        public async Task GetFilteredBosstiaryCreaturesAsync_IgnoresMalformedBosstiaryEntries()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            dbContext.Creatures.AddRange(
                new Creature
                {
                    Name = "Broken Boss Entry",
                    NormalizedName = "broken boss entry",
                    LastUpdated = DateTime.UtcNow,
                    BestiaryJson = """{"bosstiaryCategorySlug":"|-abilities------=-{{ability-list|"}"""
                },
                new Creature
                {
                    Name = "Grand Master Oberon",
                    NormalizedName = "grand master oberon",
                    LastUpdated = DateTime.UtcNow,
                    BestiaryJson = """{"bosstiaryCategorySlug":"archfoe","bosstiaryCategory":"Archfoe"}"""
                });

            await dbContext.SaveChangesAsync();

            BosstiaryDataBaseService service = CreateService(dbContext);

            BosstiaryFilteredCreaturesResponse result = await service.GetFilteredBosstiaryCreaturesAsync();

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(["Grand Master Oberon"], result.Items.Select(entry => entry.CreatureName).ToArray());
        }

        private static TibiaDbContext CreateDbContext()
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                                                       .Options;

            return new TibiaDbContext(options);
        }

        private static BosstiaryDataBaseService CreateService(TibiaDbContext dbContext)
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddHybridCache();
            services.AddSingleton(new CachingOptions());

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            return ActivatorUtilities.CreateInstance<BosstiaryDataBaseService>(serviceProvider, dbContext);
        }
    }
}
