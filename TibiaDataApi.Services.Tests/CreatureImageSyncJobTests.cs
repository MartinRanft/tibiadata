using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.BackgroundJobs;

namespace TibiaDataApi.Services.Tests
{
    public sealed class CreatureImageSyncJobTests
    {
        [Fact]
        public async Task Invoke_DrainsMultipleBatchesWithinSingleJobRun()
        {
            StubCreatureImageSyncService creatureImageSyncService = new(
            [
                new CreatureImageSyncBatchResult(25, 20, 5, 0, 0),
                new CreatureImageSyncBatchResult(10, 8, 2, 0, 0),
                new CreatureImageSyncBatchResult(0, 0, 0, 0, 0)
            ]);
            StubBackgroundJobOrchestrator backgroundJobOrchestrator = new();

            CreatureImageSyncJob job = new(
                creatureImageSyncService,
                backgroundJobOrchestrator,
                new BackgroundJobOptions
                {
                    CreatureImageSync = new CreatureImageSyncBackgroundJobOptions
                    {
                        Enabled = true,
                        BatchSize = 25
                    }
                },
                NullLogger<CreatureImageSyncJob>.Instance);

            await job.Invoke();

            BackgroundJobExecutionResult result = Assert.IsType<BackgroundJobExecutionResult>(backgroundJobOrchestrator.LastExecutionResult);

            Assert.Equal(3, creatureImageSyncService.CallCount);
            Assert.Equal(35, result.ProcessedCount);
            Assert.Equal(35, result.SucceededCount);
            Assert.Equal(BackgroundJobExecutionState.Completed, result.Status);
            Assert.Contains("BatchesProcessed", result.MetadataJson);
        }

        private sealed class StubCreatureImageSyncService(IReadOnlyList<CreatureImageSyncBatchResult> results) : ICreatureImageSyncService
        {
            private int _currentIndex;

            public List<int> BatchSizes { get; } = [];

            public int CallCount => BatchSizes.Count;

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
                BatchSizes.Add(batchSize);

                if(_currentIndex >= results.Count)
                {
                    return Task.FromResult(new CreatureImageSyncBatchResult(0, 0, 0, 0, 0));
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