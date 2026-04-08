using System.Text.Json;

using Coravel.Invocable;

using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.BackgroundJobs;

namespace TibiaDataApi.Services.Assets
{
    public sealed class CreatureImageSyncJob(
        ICreatureImageSyncService creatureImageSyncService,
        IBackgroundJobOrchestrator backgroundJobOrchestrator,
        BackgroundJobOptions backgroundJobOptions,
        ILogger<CreatureImageSyncJob> logger) : IInvocable
    {
        private readonly BackgroundJobOptions _backgroundJobOptions = backgroundJobOptions;
        private readonly IBackgroundJobOrchestrator _backgroundJobOrchestrator = backgroundJobOrchestrator;
        private readonly ICreatureImageSyncService _creatureImageSyncService = creatureImageSyncService;
        private readonly ILogger<CreatureImageSyncJob> _logger = logger;

        public async Task Invoke()
        {
            if(!_backgroundJobOptions.CreatureImageSync.Enabled)
            {
                _logger.LogInformation("Creature image sync background job is disabled by configuration.");
                return;
            }

            await _backgroundJobOrchestrator.RunAsync(
                new BackgroundJobDefinition(
                    "creature-image-sync",
                    "Scheduler",
                    _backgroundJobOptions.CreatureImageSync.LeaseName,
                    _backgroundJobOptions.CreatureImageSync.TimeoutMinutes,
                    _backgroundJobOptions.CreatureImageSync.LeaseDurationMinutes,
                    _backgroundJobOptions.CreatureImageSync.LeaseRenewalSeconds,
                    _backgroundJobOptions.CreatureImageSync.MaxLeaseRenewalFailures),
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
                        CreatureImageSyncBatchResult result = await _creatureImageSyncService.SyncPendingAsync(
                            _backgroundJobOptions.CreatureImageSync.BatchSize,
                            cancellationToken).ConfigureAwait(false);

                        if(result.Processed == 0)
                        {
                            if(totalProcessed == 0)
                            {
                                return new BackgroundJobExecutionResult(
                                    BackgroundJobExecutionState.Skipped,
                                    "No pending creature image sync entries were available.",
                                    SkippedCount: result.Skipped);
                            }

                            return new BackgroundJobExecutionResult(
                                totalFailed > 0 ? BackgroundJobExecutionState.Failed : BackgroundJobExecutionState.Completed,
                                totalFailed > 0
                                ? "Creature image sync batches completed with failures."
                                : "Creature image sync queue drained.",
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