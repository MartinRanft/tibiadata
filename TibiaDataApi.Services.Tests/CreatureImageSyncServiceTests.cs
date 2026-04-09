using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.BackgroundJobs;
using TibiaDataApi.Services.Entities.Assets;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class CreatureImageSyncServiceTests
    {
        [Fact]
        public async Task QueuePrimaryImageSyncAsync_AddsPendingQueueEntry_WhenCreatureHasNoPrimaryAsset()
        {
            ServiceProvider serviceProvider = CreateServiceProvider(Guid.NewGuid().ToString("N"), new StubCreatureImageAssetService());

            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            ICreatureImageSyncService service = scope.ServiceProvider.GetRequiredService<ICreatureImageSyncService>();

            Creature creature = new()
            {
                Name = "Scarab",
                NormalizedName = "scarab"
            };

            dbContext.Creatures.Add(creature);
            await dbContext.SaveChangesAsync();

            await service.QueuePrimaryImageSyncAsync(creature.Id, "Scarab", false);

            CreatureImageSyncQueueEntry? queueEntry = await dbContext.CreatureImageSyncQueueEntries.SingleOrDefaultAsync();

            Assert.NotNull(queueEntry);
            Assert.Equal(creature.Id, queueEntry!.CreatureId);
            Assert.Equal("Scarab", queueEntry.WikiPageTitle);
            Assert.Equal(ItemImageSyncState.Pending, queueEntry.Status);
        }

        [Fact]
        public async Task SyncPendingAsync_MarksEntryAsSucceeded_WhenAssetWasCreated()
        {
            string databaseName = Guid.NewGuid().ToString("N");
            ServiceProvider serviceProvider = CreateServiceProvider(databaseName, new StubCreatureImageAssetService(true));

            await using (AsyncServiceScope seedScope = serviceProvider.CreateAsyncScope())
            {
                TibiaDbContext dbContext = seedScope.ServiceProvider.GetRequiredService<TibiaDbContext>();

                Creature creature = new()
                {
                    Name = "Scarab",
                    NormalizedName = "scarab"
                };

                dbContext.Creatures.Add(creature);
                await dbContext.SaveChangesAsync();

                dbContext.CreatureImageSyncQueueEntries.Add(new CreatureImageSyncQueueEntry
                {
                    CreatureId = creature.Id,
                    WikiPageTitle = "Scarab",
                    Status = ItemImageSyncState.Pending,
                    RequestedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }

            await using (AsyncServiceScope runScope = serviceProvider.CreateAsyncScope())
            {
                ICreatureImageSyncService service = runScope.ServiceProvider.GetRequiredService<ICreatureImageSyncService>();
                CreatureImageSyncBatchResult result = await service.SyncPendingAsync(10);

                Assert.Equal(1, result.Processed);
                Assert.Equal(1, result.Succeeded);
            }

            await using (AsyncServiceScope verifyScope = serviceProvider.CreateAsyncScope())
            {
                TibiaDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<TibiaDbContext>();
                CreatureImageSyncQueueEntry queueEntry = await dbContext.CreatureImageSyncQueueEntries.SingleAsync();

                Assert.Equal(ItemImageSyncState.Succeeded, queueEntry.Status);
                Assert.NotNull(queueEntry.LastCompletedAt);
            }
        }

        private static ServiceProvider CreateServiceProvider(
            string databaseName,
            ICreatureImageAssetService creatureImageAssetService)
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddSingleton(new BackgroundJobOptions());
            services.AddDbContext<TibiaDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.AddScoped<ICreatureImageAssetService>(_ => creatureImageAssetService);
            services.AddScoped<ICreatureImageSyncService, CreatureImageSyncService>();

            return services.BuildServiceProvider();
        }

        private sealed class StubCreatureImageAssetService(bool createPrimaryAsset = false) : ICreatureImageAssetService
        {
            public Task SyncPrimaryImageAsync(
                TibiaDbContext db,
                Creature creature,
                string wikiPageTitle,
                CancellationToken cancellationToken = default)
            {
                if(!createPrimaryAsset)
                {
                    return Task.CompletedTask;
                }

                Asset asset = new()
                {
                    StorageKey = $"creatures/{creature.Id}/primary.gif",
                    FileName = "primary.gif"
                };

                db.Assets.Add(asset);
                db.CreatureAssets.Add(new CreatureAsset
                {
                    CreatureId = creature.Id,
                    Creature = creature,
                    Asset = asset,
                    AssetKind = AssetKind.PrimaryImage,
                    IsPrimary = true,
                    SortOrder = 0
                });

                return Task.CompletedTask;
            }
        }
    }
}