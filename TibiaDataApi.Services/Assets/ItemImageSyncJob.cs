using System.Text.Json;

using Coravel.Invocable;

using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.BackgroundJobs;

namespace TibiaDataApi.Services.Assets
{
    public sealed class ItemImageSyncJob(
        IItemImageSyncService itemImageSyncService,
        IBackgroundJobOrchestrator backgroundJobOrchestrator,
        BackgroundJobOptions backgroundJobOptions,
        ILogger<ItemImageSyncJob> logger) : IInvocable
    {
        private readonly BackgroundJobOptions _backgroundJobOptions = backgroundJobOptions;
        private readonly IBackgroundJobOrchestrator _backgroundJobOrchestrator = backgroundJobOrchestrator;
        private readonly IItemImageSyncService _itemImageSyncService = itemImageSyncService;
        private readonly ILogger<ItemImageSyncJob> _logger = logger;

        public async Task Invoke()
        {
            if(!_backgroundJobOptions.ItemImageSync.Enabled)
            {
                _logger.LogInformation("Item image sync background job is disabled by configuration.");
                return;
            }

            await _backgroundJobOrchestrator.RunAsync(
                new BackgroundJobDefinition(
                    "item-image-sync",
                    "Scheduler",
                    _backgroundJobOptions.ItemImageSync.LeaseName,
                    _backgroundJobOptions.ItemImageSync.TimeoutMinutes,
                    _backgroundJobOptions.ItemImageSync.LeaseDurationMinutes,
                    _backgroundJobOptions.ItemImageSync.LeaseRenewalSeconds,
                    _backgroundJobOptions.ItemImageSync.MaxLeaseRenewalFailures),
                async cancellationToken =>
                {
                    int totalProcessed = 0;
                    int totalSucceeded = 0;
                    int totalMissing = 0;
                    int totalFailed = 0;
                    int totalSkipped = 0;
                    int batchCount = 0;

                    while (true)
                    {
                        ItemImageSyncBatchResult result = await _itemImageSyncService.SyncPendingAsync(
                            _backgroundJobOptions.ItemImageSync.BatchSize,
                            cancellationToken).ConfigureAwait(false);

                        if(result.Processed == 0)
                        {
                            if(totalProcessed == 0)
                            {
                                return new BackgroundJobExecutionResult(
                                    BackgroundJobExecutionState.Skipped,
                                    "No pending item image sync entries were available.",
                                    SkippedCount: result.Skipped);
                            }

                            return new BackgroundJobExecutionResult(
                                totalFailed > 0 ? BackgroundJobExecutionState.Failed : BackgroundJobExecutionState.Completed,
                                totalFailed > 0
                                ? "Item image sync batches completed with failures."
                                : "Item image sync queue drained.",
                                totalProcessed,
                                totalSucceeded + totalMissing,
                                totalFailed,
                                totalSkipped,
                                JsonSerializer.Serialize(new
                                {
                                    BatchesProcessed = batchCount,
                                    MissingCount = totalMissing
                                }));
                        }

                        batchCount++;
                        totalProcessed += result.Processed;
                        totalSucceeded += result.Succeeded;
                        totalMissing += result.Missing;
                        totalFailed += result.Failed;
                        totalSkipped += result.Skipped;
                    }
                }).ConfigureAwait(false);
        }
    }
}