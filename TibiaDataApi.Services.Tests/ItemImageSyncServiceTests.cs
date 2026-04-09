using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.BackgroundJobs;
using TibiaDataApi.Services.Entities.Assets;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class ItemImageSyncServiceTests
    {
        [Fact]
        public async Task QueuePrimaryImageSyncAsync_AddsPendingQueueEntry_WhenItemHasNoPrimaryAsset()
        {
            ServiceProvider serviceProvider = CreateServiceProvider(Guid.NewGuid().ToString("N"), new StubItemImageAssetService());

            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            IItemImageSyncService service = scope.ServiceProvider.GetRequiredService<IItemImageSyncService>();

            Item item = new()
            {
                Name = "Eldritch Rod",
                NormalizedName = "eldritch rod"
            };

            dbContext.Items.Add(item);
            await dbContext.SaveChangesAsync();

            await service.QueuePrimaryImageSyncAsync(item.Id, "Eldritch Rod", false);

            ItemImageSyncQueueEntry? queueEntry = await dbContext.ItemImageSyncQueueEntries.SingleOrDefaultAsync();

            Assert.NotNull(queueEntry);
            Assert.Equal(item.Id, queueEntry!.ItemId);
            Assert.Equal("Eldritch Rod", queueEntry.WikiPageTitle);
            Assert.Equal(ItemImageSyncState.Pending, queueEntry.Status);
        }

        [Fact]
        public async Task SyncPendingAsync_MarksEntryAsSucceeded_WhenAssetWasCreated()
        {
            string databaseName = Guid.NewGuid().ToString("N");
            ServiceProvider serviceProvider = CreateServiceProvider(databaseName, new StubItemImageAssetService(true));

            await using (AsyncServiceScope seedScope = serviceProvider.CreateAsyncScope())
            {
                TibiaDbContext dbContext = seedScope.ServiceProvider.GetRequiredService<TibiaDbContext>();

                Item item = new()
                {
                    Name = "Eldritch Rod",
                    NormalizedName = "eldritch rod"
                };

                dbContext.Items.Add(item);
                await dbContext.SaveChangesAsync();

                dbContext.ItemImageSyncQueueEntries.Add(new ItemImageSyncQueueEntry
                {
                    ItemId = item.Id,
                    WikiPageTitle = "Eldritch Rod",
                    Status = ItemImageSyncState.Pending,
                    RequestedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }

            await using (AsyncServiceScope runScope = serviceProvider.CreateAsyncScope())
            {
                IItemImageSyncService service = runScope.ServiceProvider.GetRequiredService<IItemImageSyncService>();
                ItemImageSyncBatchResult result = await service.SyncPendingAsync(10);

                Assert.Equal(1, result.Processed);
                Assert.Equal(1, result.Succeeded);
            }

            await using (AsyncServiceScope verifyScope = serviceProvider.CreateAsyncScope())
            {
                TibiaDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<TibiaDbContext>();
                ItemImageSyncQueueEntry queueEntry = await dbContext.ItemImageSyncQueueEntries.SingleAsync();

                Assert.Equal(ItemImageSyncState.Succeeded, queueEntry.Status);
                Assert.NotNull(queueEntry.LastCompletedAt);
            }
        }

        [Fact]
        public async Task SyncPendingAsync_ProcessesClaimedEntriesInParallel()
        {
            string databaseName = Guid.NewGuid().ToString("N");
            ParallelImageSyncProbe probe = new(3);
            ServiceProvider serviceProvider = CreateServiceProvider(databaseName, probe, 0);

            await using (AsyncServiceScope seedScope = serviceProvider.CreateAsyncScope())
            {
                TibiaDbContext dbContext = seedScope.ServiceProvider.GetRequiredService<TibiaDbContext>();

                for (int index = 1; index <= 3; index++)
                {
                    Item item = new()
                    {
                        Name = $"Item {index}",
                        NormalizedName = $"item {index}"
                    };

                    dbContext.Items.Add(item);
                    await dbContext.SaveChangesAsync();

                    dbContext.ItemImageSyncQueueEntries.Add(new ItemImageSyncQueueEntry
                    {
                        ItemId = item.Id,
                        WikiPageTitle = item.Name,
                        Status = ItemImageSyncState.Pending,
                        RequestedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await dbContext.SaveChangesAsync();
            }

            await using AsyncServiceScope runScope = serviceProvider.CreateAsyncScope();
            IItemImageSyncService service = runScope.ServiceProvider.GetRequiredService<IItemImageSyncService>();

            Task<ItemImageSyncBatchResult> syncTask = service.SyncPendingAsync(3);

            await probe.WaitForAllStartedAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(3, probe.MaxConcurrentObserved);

            probe.ReleaseAll();

            ItemImageSyncBatchResult result = await syncTask.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(3, result.Processed);
            Assert.Equal(3, result.Succeeded);
        }

        [Fact]
        public async Task QueuePrimaryImageSyncAsync_CollapsesConcurrentRequests_ForSameItem()
        {
            string databaseName = Guid.NewGuid().ToString("N");
            ServiceProvider serviceProvider = CreateServiceProvider(databaseName, new StubItemImageAssetService());

            int itemId;
            await using (AsyncServiceScope seedScope = serviceProvider.CreateAsyncScope())
            {
                TibiaDbContext dbContext = seedScope.ServiceProvider.GetRequiredService<TibiaDbContext>();
                Item item = new()
                {
                    Name = "Concurrent Rod",
                    NormalizedName = "concurrent rod"
                };

                dbContext.Items.Add(item);
                await dbContext.SaveChangesAsync();
                itemId = item.Id;
            }

            await using AsyncServiceScope firstScope = serviceProvider.CreateAsyncScope();
            await using AsyncServiceScope secondScope = serviceProvider.CreateAsyncScope();
            IItemImageSyncService firstService = firstScope.ServiceProvider.GetRequiredService<IItemImageSyncService>();
            IItemImageSyncService secondService = secondScope.ServiceProvider.GetRequiredService<IItemImageSyncService>();

            await Task.WhenAll(
                firstService.QueuePrimaryImageSyncAsync(itemId, "Concurrent Rod", false),
                secondService.QueuePrimaryImageSyncAsync(itemId, "Concurrent Rod (Updated)", true));

            await using AsyncServiceScope verifyScope = serviceProvider.CreateAsyncScope();
            TibiaDbContext verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            List<ItemImageSyncQueueEntry> queueEntries = await verifyDbContext.ItemImageSyncQueueEntries.ToListAsync();

            ItemImageSyncQueueEntry queueEntry = Assert.Single(queueEntries);
            Assert.Equal(itemId, queueEntry.ItemId);
            Assert.Equal("Concurrent Rod (Updated)", queueEntry.WikiPageTitle);
            Assert.Equal(ItemImageSyncState.Pending, queueEntry.Status);
        }

        private static ServiceProvider CreateServiceProvider(
            string databaseName,
            IItemImageAssetService itemImageAssetService,
            int maxParallelWorkers = 0)
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddSingleton(new BackgroundJobOptions
            {
                ItemImageSync = new ItemImageSyncBackgroundJobOptions
                {
                    MaxParallelWorkers = maxParallelWorkers
                }
            });
            services.AddDbContext<TibiaDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.AddScoped<IItemImageAssetService>(_ => itemImageAssetService);
            services.AddScoped<IItemImageSyncService, ItemImageSyncService>();

            return services.BuildServiceProvider();
        }

        private sealed class StubItemImageAssetService(bool createPrimaryAsset = false) : IItemImageAssetService
        {
            public Task SyncPrimaryImageAsync(
                TibiaDbContext db,
                Item item,
                string wikiPageTitle,
                CancellationToken cancellationToken = default)
            {
                if(!createPrimaryAsset)
                {
                    return Task.CompletedTask;
                }

                Asset asset = new()
                {
                    StorageKey = $"items/{item.Id}/primary.gif",
                    FileName = "primary.gif"
                };

                db.Assets.Add(asset);
                db.ItemAssets.Add(new ItemAsset
                {
                    ItemId = item.Id,
                    Item = item,
                    Asset = asset,
                    AssetKind = AssetKind.PrimaryImage,
                    IsPrimary = true,
                    SortOrder = 0
                });

                return Task.CompletedTask;
            }
        }

        private sealed class ParallelImageSyncProbe(int expectedCount) : IItemImageAssetService
        {
            private readonly TaskCompletionSource _allStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int _activeCount;
            private int _maxConcurrentObserved;
            private int _startedCount;

            public int MaxConcurrentObserved => _maxConcurrentObserved;

            public async Task SyncPrimaryImageAsync(
                TibiaDbContext db,
                Item item,
                string wikiPageTitle,
                CancellationToken cancellationToken = default)
            {
                int activeCount = Interlocked.Increment(ref _activeCount);
                InterlockedExtensions.Max(ref _maxConcurrentObserved, activeCount);

                if(Interlocked.Increment(ref _startedCount) >= expectedCount)
                {
                    _allStarted.TrySetResult();
                }

                try
                {
                    await _release.Task.WaitAsync(cancellationToken);

                    Asset asset = new()
                    {
                        StorageKey = $"items/{item.Id}/primary.gif",
                        FileName = "primary.gif"
                    };

                    db.Assets.Add(asset);
                    db.ItemAssets.Add(new ItemAsset
                    {
                        ItemId = item.Id,
                        Item = item,
                        Asset = asset,
                        AssetKind = AssetKind.PrimaryImage,
                        IsPrimary = true,
                        SortOrder = 0
                    });
                }
                finally
                {
                    Interlocked.Decrement(ref _activeCount);
                }
            }

            public Task WaitForAllStartedAsync()
            {
                return _allStarted.Task;
            }

            public void ReleaseAll()
            {
                _release.TrySetResult();
            }
        }

        private static class InterlockedExtensions
        {
            public static void Max(ref int target, int value)
            {
                int snapshot;

                do
                {
                    snapshot = target;

                    if(snapshot >= value)
                    {
                        return;
                    }
                } while (Interlocked.CompareExchange(ref target, value, snapshot) != snapshot);
            }
        }
    }
}