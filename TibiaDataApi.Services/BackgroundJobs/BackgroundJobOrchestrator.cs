using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Entities.Monitoring;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper.Runtime;

namespace TibiaDataApi.Services.BackgroundJobs
{
    public sealed class BackgroundJobOrchestrator(
        IServiceScopeFactory serviceScopeFactory,
        IScraperExecutionLeaseService executionLeaseService,
        ILogger<BackgroundJobOrchestrator> logger) : IBackgroundJobOrchestrator
    {
        private readonly IScraperExecutionLeaseService _executionLeaseService = executionLeaseService;
        private readonly ILogger<BackgroundJobOrchestrator> _logger = logger;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        public async Task<BackgroundJobRunResult> RunAsync(
            BackgroundJobDefinition definition,
            Func<CancellationToken, Task<BackgroundJobExecutionResult>> handler,
            CancellationToken cancellationToken = default)
        {
            string? leaseOwnerId = null;

            if(!string.IsNullOrWhiteSpace(definition.LeaseName))
            {
                leaseOwnerId = CreateLeaseOwnerId(definition.JobName, definition.TriggeredBy);
                ScraperExecutionLeaseAcquireResult acquireResult = await _executionLeaseService.TryAcquireAsync(
                    definition.LeaseName,
                    leaseOwnerId,
                    GetLeaseDuration(definition),
                    cancellationToken).ConfigureAwait(false);

                if(!acquireResult.Acquired)
                {
                    string message = acquireResult.ExpiresAt.HasValue
                    ? $"Skipped background job because lease {definition.LeaseName} is held until {acquireResult.ExpiresAt.Value:u}."
                    : $"Skipped background job because lease {definition.LeaseName} is already held.";

                    int executionId = await CreateExecutionAsync(
                        definition,
                        BackgroundJobExecutionState.Skipped,
                        message,
                        null,
                        DateTime.UtcNow,
                        DateTime.UtcNow,
                        new BackgroundJobExecutionResult(
                            BackgroundJobExecutionState.Skipped,
                            message,
                            SkippedCount: 1),
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("{JobName} skipped because another instance already holds the lease.", definition.JobName);
                    return new BackgroundJobRunResult(false, BackgroundJobExecutionState.Skipped, message, executionId, null);
                }
            }

            DateTime startedAt = DateTime.UtcNow;
            int executionRecordId = await CreateExecutionAsync(
                definition,
                BackgroundJobExecutionState.Running,
                "Background job started.",
                leaseOwnerId,
                startedAt,
                null,
                null,
                cancellationToken).ConfigureAwait(false);

            using CancellationTokenSource internalCancellationTokenSource = new();
            CancellationTokenSource? timeoutCancellationTokenSource = null;
            CancellationTokenSource? linkedCancellationTokenSource = null;
            string? cancellationReason = null;
            Task leaseRenewalTask = Task.CompletedTask;

            try
            {
                if(definition.TimeoutMinutes > 0)
                {
                    timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(definition.TimeoutMinutes));
                    linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        internalCancellationTokenSource.Token,
                        timeoutCancellationTokenSource.Token);
                }
                else
                {
                    linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        internalCancellationTokenSource.Token);
                }

                leaseRenewalTask = RunLeaseRenewalLoopAsync(
                    definition,
                    leaseOwnerId,
                    internalCancellationTokenSource,
                    () => cancellationReason,
                    reason => cancellationReason = reason,
                    linkedCancellationTokenSource.Token);

                BackgroundJobExecutionResult executionResult;

                try
                {
                    executionResult = await handler(linkedCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (linkedCancellationTokenSource.IsCancellationRequested)
                {
                    bool timedOut = timeoutCancellationTokenSource?.IsCancellationRequested == true &&
                                    !cancellationToken.IsCancellationRequested;

                    string message = !string.IsNullOrWhiteSpace(cancellationReason)
                    ? cancellationReason
                    : timedOut
                    ? $"Background job timed out after {definition.TimeoutMinutes} minute(s)."
                    : "Background job was cancelled.";

                    executionResult = new BackgroundJobExecutionResult(
                        BackgroundJobExecutionState.Cancelled,
                        message);
                }

                try
                {
                    internalCancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }

                await AwaitBackgroundTaskAsync(leaseRenewalTask, definition.JobName).ConfigureAwait(false);

                DateTime finishedAt = DateTime.UtcNow;
                await UpdateExecutionAsync(
                    executionRecordId,
                    executionResult,
                    finishedAt,
                    cancellationToken).ConfigureAwait(false);

                LogExecution(definition.JobName, executionResult, executionRecordId);

                return new BackgroundJobRunResult(
                    executionResult.Status is BackgroundJobExecutionState.Completed,
                    executionResult.Status,
                    executionResult.Message,
                    executionRecordId,
                    executionResult);
            }
            catch (Exception ex)
            {
                DateTime finishedAt = DateTime.UtcNow;
                BackgroundJobExecutionResult failedResult = new(
                    BackgroundJobExecutionState.Failed,
                    ex.Message,
                    FailedCount: 1);

                await UpdateExecutionAsync(executionRecordId, failedResult, finishedAt, CancellationToken.None).ConfigureAwait(false);
                _logger.LogError(ex, "Background job {JobName} failed.", definition.JobName);

                return new BackgroundJobRunResult(false, BackgroundJobExecutionState.Failed, ex.Message, executionRecordId, failedResult);
            }
            finally
            {
                try
                {
                    internalCancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }

                await AwaitBackgroundTaskAsync(leaseRenewalTask, definition.JobName).ConfigureAwait(false);

                linkedCancellationTokenSource?.Dispose();
                timeoutCancellationTokenSource?.Dispose();

                if(!string.IsNullOrWhiteSpace(definition.LeaseName) && !string.IsNullOrWhiteSpace(leaseOwnerId))
                {
                    await ReleaseLeaseSafelyAsync(definition.LeaseName!, leaseOwnerId).ConfigureAwait(false);
                }
            }
        }

        private async Task<int> CreateExecutionAsync(
            BackgroundJobDefinition definition,
            BackgroundJobExecutionState status,
            string message,
            string? leaseOwnerId,
            DateTime startedAt,
            DateTime? finishedAt,
            BackgroundJobExecutionResult? result,
            CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            BackgroundJobExecution execution = new()
            {
                JobName = definition.JobName,
                TriggeredBy = definition.TriggeredBy,
                Status = status,
                LeaseName = definition.LeaseName,
                LeaseOwnerId = leaseOwnerId,
                Message = message,
                ProcessedCount = result?.ProcessedCount ?? 0,
                SucceededCount = result?.SucceededCount ?? 0,
                FailedCount = result?.FailedCount ?? 0,
                SkippedCount = result?.SkippedCount ?? 0,
                MetadataJson = result?.MetadataJson,
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                DurationMs = finishedAt.HasValue ? (finishedAt.Value - startedAt).TotalMilliseconds : null,
                UpdatedAt = finishedAt ?? startedAt
            };

            db.BackgroundJobExecutions.Add(execution);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return execution.Id;
        }

        private async Task UpdateExecutionAsync(
            int executionId,
            BackgroundJobExecutionResult result,
            DateTime finishedAt,
            CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            BackgroundJobExecution? execution = await db.BackgroundJobExecutions
                                                        .FirstOrDefaultAsync(entry => entry.Id == executionId, cancellationToken)
                                                        .ConfigureAwait(false);

            if(execution is null)
            {
                return;
            }

            execution.Status = result.Status;
            execution.Message = result.Message;
            execution.ProcessedCount = result.ProcessedCount;
            execution.SucceededCount = result.SucceededCount;
            execution.FailedCount = result.FailedCount;
            execution.SkippedCount = result.SkippedCount;
            execution.MetadataJson = result.MetadataJson;
            execution.FinishedAt = finishedAt;
            execution.DurationMs = (finishedAt - execution.StartedAt).TotalMilliseconds;
            execution.UpdatedAt = finishedAt;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task RunLeaseRenewalLoopAsync(
            BackgroundJobDefinition definition,
            string? leaseOwnerId,
            CancellationTokenSource internalCancellationTokenSource,
            Func<string?> currentCancellationReasonAccessor,
            Action<string> setCancellationReason,
            CancellationToken cancellationToken)
        {
            if(string.IsNullOrWhiteSpace(definition.LeaseName) || string.IsNullOrWhiteSpace(leaseOwnerId))
            {
                return;
            }

            int renewalIntervalSeconds = Math.Max(5, definition.LeaseRenewalSeconds);
            int maxRenewalFailures = Math.Max(1, definition.MaxLeaseRenewalFailures);
            int consecutiveFailures = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(renewalIntervalSeconds), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                bool renewed;

                try
                {
                    renewed = await _executionLeaseService.RenewAsync(
                        definition.LeaseName!,
                        leaseOwnerId,
                        GetLeaseDuration(definition),
                        cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to renew lease for background job {JobName}.", definition.JobName);
                    renewed = false;
                }

                if(renewed)
                {
                    consecutiveFailures = 0;
                    continue;
                }

                consecutiveFailures++;

                if(consecutiveFailures < maxRenewalFailures)
                {
                    continue;
                }

                string reason = string.IsNullOrWhiteSpace(currentCancellationReasonAccessor())
                ? $"Background job lease {definition.LeaseName} was lost."
                : currentCancellationReasonAccessor()!;

                setCancellationReason(reason);

                try
                {
                    internalCancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }
        }

        private async Task ReleaseLeaseSafelyAsync(string leaseName, string leaseOwnerId)
        {
            try
            {
                await _executionLeaseService.ReleaseAsync(leaseName, leaseOwnerId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release background job lease {LeaseName}.", leaseName);
            }
        }

        private async Task AwaitBackgroundTaskAsync(Task task, string jobName)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background lease renewal task for job {JobName} ended with an error.", jobName);
            }
        }

        private void LogExecution(string jobName, BackgroundJobExecutionResult result, int executionId)
        {
            if(result.Status == BackgroundJobExecutionState.Failed)
            {
                _logger.LogError(
                    "Background job {JobName} failed. ExecutionId={ExecutionId}. Message={Message}",
                    jobName,
                    executionId,
                    result.Message);

                return;
            }

            _logger.LogInformation(
                "Background job {JobName} finished with status {Status}. ExecutionId={ExecutionId}. Processed={Processed}, Succeeded={Succeeded}, Failed={Failed}, Skipped={Skipped}.",
                jobName,
                result.Status,
                executionId,
                result.ProcessedCount,
                result.SucceededCount,
                result.FailedCount,
                result.SkippedCount);
        }

        private static TimeSpan GetLeaseDuration(BackgroundJobDefinition definition)
        {
            return TimeSpan.FromMinutes(Math.Max(1, definition.LeaseDurationMinutes));
        }

        private static string CreateLeaseOwnerId(string jobName, string triggeredBy)
        {
            return $"{Environment.MachineName}:{jobName}:{triggeredBy}:{Guid.NewGuid():N}";
        }
    }
}