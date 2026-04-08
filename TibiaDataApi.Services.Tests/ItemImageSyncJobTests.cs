using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.BackgroundJobs;

namespace TibiaDataApi.Services.Tests
{
    public sealed class ItemImageSyncJobTests
    {
        [Fact]
        public async Task Invoke_DrainsMultipleBatchesWithinSingleJobRun()
        {
            StubItemImageSyncService itemImageSyncService = new(
            [
                new ItemImageSyncBatchResult(25, 20, 5, 0, 0),
                new ItemImageSyncBatchResult(10, 8, 2, 0, 0),
                new ItemImageSyncBatchResult(0, 0, 0, 0, 0)
            ]);
            StubBackgroundJobOrchestrator backgroundJobOrchestrator = new();

            ItemImageSyncJob job = new(
                itemImageSyncService,
                backgroundJobOrchestrator,
                new BackgroundJobOptions
                {
                    ItemImageSync = new ItemImageSyncBackgroundJobOptions
                    {
                        Enabled = true,
                        BatchSize = 25
                    }
                },
                NullLogger<ItemImageSyncJob>.Instance);

            await job.Invoke();

            BackgroundJobExecutionResult result = Assert.IsType<BackgroundJobExecutionResult>(backgroundJobOrchestrator.LastExecutionResult);

            Assert.Equal(3, itemImageSyncService.CallCount);
            Assert.Equal(35, result.ProcessedCount);
            Assert.Equal(35, result.SucceededCount);
            Assert.Equal(BackgroundJobExecutionState.Completed, result.Status);
            Assert.Contains("BatchesProcessed", result.MetadataJson);
        }

        [Fact]
        public async Task Invoke_ReturnsSkipped_WhenNoPendingEntriesExist()
        {
            StubItemImageSyncService itemImageSyncService = new(
            [
                new ItemImageSyncBatchResult(0, 0, 0, 0, 0)
            ]);
            StubBackgroundJobOrchestrator backgroundJobOrchestrator = new();

            ItemImageSyncJob job = new(
                itemImageSyncService,
                backgroundJobOrchestrator,
                new BackgroundJobOptions(),
                NullLogger<ItemImageSyncJob>.Instance);

            await job.Invoke();

            BackgroundJobExecutionResult result = Assert.IsType<BackgroundJobExecutionResult>(backgroundJobOrchestrator.LastExecutionResult);

            Assert.Single(itemImageSyncService.BatchSizes);
            Assert.Equal(BackgroundJobExecutionState.Skipped, result.Status);
        }

        private sealed class StubItemImageSyncService(IReadOnlyList<ItemImageSyncBatchResult> results) : IItemImageSyncService
        {
            private int _currentIndex;

            public List<int> BatchSizes { get; } = [];

            public int CallCount => BatchSizes.Count;

            public Task QueuePrimaryImageSyncAsync(
                int itemId,
                string wikiPageTitle,
                bool forceSync,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<ItemImageSyncBatchResult> SyncPendingAsync(
                int batchSize,
                CancellationToken cancellationToken = default)
            {
                BatchSizes.Add(batchSize);

                if(_currentIndex >= results.Count)
                {
                    return Task.FromResult(new ItemImageSyncBatchResult(0, 0, 0, 0, 0));
                }

                return Task.FromResult(results[_currentIndex++]);
            }
        }

        private sealed class StubBackgroundJobOrchestrator : IBackgroundJobOrchestrator
        {
            public BackgroundJobExecutionResult? LastExecutionResult { get; private set; }

            public async Task<BackgroundJobRunResult> RunAsync(
                BackgroundJobDefinition definition,
                Func<CancellationToken, Task<BackgroundJobExecutionResult>> handler,
                CancellationToken cancellationToken = default)
            {
                LastExecutionResult = await handler(cancellationToken);
                return new BackgroundJobRunResult(
                    LastExecutionResult.Status == BackgroundJobExecutionState.Completed,
                    LastExecutionResult.Status,
                    LastExecutionResult.Message,
                    null,
                    LastExecutionResult);
            }
        }
    }
}