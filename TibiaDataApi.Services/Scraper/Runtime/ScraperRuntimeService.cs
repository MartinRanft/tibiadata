using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Scraper.Runtime
{
    public sealed class ScraperRuntimeService(
        IServiceScopeFactory serviceScopeFactory,
        ICacheInvalidationService cacheInvalidationService,
        ScraperRuntimeOptions runtimeOptions,
        IScraperExecutionLeaseService scraperExecutionLeaseService,
        ILogger<ScraperRuntimeService> logger) : IScraperRuntimeService
    {
        private readonly ICacheInvalidationService _cacheInvalidationService = cacheInvalidationService;
        private readonly ILogger<ScraperRuntimeService> _logger = logger;
        private readonly ScraperRuntimeOptions _runtimeOptions = runtimeOptions;
        private readonly IScraperExecutionLeaseService _scraperExecutionLeaseService = scraperExecutionLeaseService;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly object _sync = new();

        private CancellationTokenSource? _activeRunCancellationTokenSource;
        private Task? _activeRunTask;
        private RuntimeState _state = new();

        public ScraperRuntimeStatus GetStatus()
        {
            lock (_sync)
            {
                return CreateStatusSnapshot();
            }
        }

        public async Task<ScraperStartResult> StartAsync(
            ScraperRunRequest request,
            CancellationToken cancellationToken = default)
        {
            ScraperRunRequest normalizedRequest = NormalizeRequest(request, "Manual");
            IReadOnlyList<ScraperDescriptor> matchingScrapers =
            await GetMatchingScrapersAsync(normalizedRequest).ConfigureAwait(false);

            if(matchingScrapers.Count == 0)
            {
                return new ScraperStartResult(false, "No matching scrapers found for the requested selection.", GetStatus());
            }

            RunPreparationResult preparation = await PrepareRunAsync(
                normalizedRequest,
                matchingScrapers,
                "Scraper run started.",
                cancellationToken).ConfigureAwait(false);

            return new ScraperStartResult(preparation.Started, preparation.Message, preparation.Status);
        }

        public async Task<ScraperStopResult> StopAsync(
            ScraperStopRequest request,
            CancellationToken cancellationToken = default)
        {
            CancellationTokenSource? runCancellationTokenSource;
            IReadOnlyList<int> activeScrapeLogIds;

            lock (_sync)
            {
                if(!_state.IsRunning || _activeRunCancellationTokenSource is null)
                {
                    return new ScraperStopResult(false, "No scraper run is currently active.", CreateStatusSnapshot());
                }

                if(_state.StopRequested)
                {
                    return new ScraperStopResult(true, "Stop has already been requested for the active scraper run.", CreateStatusSnapshot());
                }

                _state.StopRequested = true;
                _state.StopReason = NormalizeOptional(request.Reason) ?? "Stop requested.";
                _state.LastMessage = "Stop requested for all active scraper tasks.";

                runCancellationTokenSource = _activeRunCancellationTokenSource;
                activeScrapeLogIds = _state.ActiveScrapers
                                           .Where(entry => entry.ScrapeLogId.HasValue)
                                           .Select(entry => entry.ScrapeLogId!.Value)
                                           .ToList();
            }

            try
            {
                runCancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            if(activeScrapeLogIds.Count > 0)
            {
                await MarkCancellationRequestedAsync(activeScrapeLogIds, request, cancellationToken).ConfigureAwait(false);
            }

            return new ScraperStopResult(true, "Stop requested for all active scraper tasks.", GetStatus());
        }

        public async Task<ScraperScheduledRunResult> RunScheduledAsync(CancellationToken cancellationToken = default)
        {
            ScraperRunRequest request = new(TriggeredBy: "Scheduler");
            IReadOnlyList<ScraperDescriptor> matchingScrapers =
            await GetMatchingScrapersAsync(request).ConfigureAwait(false);

            if(matchingScrapers.Count == 0)
            {
                const string noScrapersMessage = "Skipping scheduled scraper run because no matching scrapers were resolved.";
                _logger.LogWarning(noScrapersMessage);
                return new ScraperScheduledRunResult(false, noScrapersMessage, GetStatus());
            }

            RunPreparationResult preparation = await PrepareRunAsync(
                request,
                matchingScrapers,
                "Scheduled scraper run started.",
                cancellationToken).ConfigureAwait(false);

            if(!preparation.Started)
            {
                _logger.LogWarning("Skipping scheduled scraper run: {Message}", preparation.Message);
                return new ScraperScheduledRunResult(false, preparation.Message, preparation.Status);
            }

            if(preparation.RunTask is not null)
            {
                await preparation.RunTask.ConfigureAwait(false);
            }

            ScraperRuntimeStatus finalStatus = GetStatus();
            string message = string.IsNullOrWhiteSpace(finalStatus.LastMessage)
            ? "Scheduled scraper run finished."
            : finalStatus.LastMessage;

            return new ScraperScheduledRunResult(true, message, finalStatus);
        }

        private async Task<RunPreparationResult> PrepareRunAsync(
            ScraperRunRequest request,
            IReadOnlyList<ScraperDescriptor> descriptors,
            string startedMessage,
            CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if(_state.IsRunning)
                {
                    string message = request.Force
                    ? "A scraper run is already active. Force start is not supported while another run is running."
                    : "A scraper run is already active.";

                    return new RunPreparationResult(false, message, CreateStatusSnapshot(), null);
                }
            }

            string leaseOwnerId = CreateLeaseOwnerId(request);
            ScraperExecutionLeaseAcquireResult leaseAcquireResult;

            try
            {
                leaseAcquireResult = await _scraperExecutionLeaseService.TryAcquireAsync(
                    _runtimeOptions.ExecutionLeaseName,
                    leaseOwnerId,
                    GetExecutionLeaseDuration(),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire scraper execution lease.");
                return new RunPreparationResult(false, "Failed to acquire the scraper execution lease.", GetStatus(), null);
            }

            if(!leaseAcquireResult.Acquired)
            {
                return new RunPreparationResult(false, BuildLeaseConflictMessage(leaseAcquireResult), GetStatus(), null);
            }

            CancellationTokenSource manualCancellationTokenSource = new();
            CancellationTokenSource timeoutCancellationTokenSource = new(GetRunTimeout());
            CancellationTokenSource executionCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                manualCancellationTokenSource.Token,
                timeoutCancellationTokenSource.Token);

            Task? runTask;
            ScraperRuntimeStatus status;
            bool releaseLease = false;

            lock (_sync)
            {
                if(_state.IsRunning)
                {
                    releaseLease = true;
                    runTask = null;
                    status = CreateStatusSnapshot();
                }
                else
                {
                    _activeRunCancellationTokenSource?.Dispose();
                    _activeRunCancellationTokenSource = manualCancellationTokenSource;

                    DateTime startedAt = DateTime.UtcNow;
                    _state = new RuntimeState
                    {
                        IsRunning = true,
                        TriggeredBy = request.TriggeredBy,
                        StartedAt = startedAt,
                        FinishedAt = null,
                        TotalScrapers = descriptors.Count,
                        CompletedScrapers = 0,
                        StopRequested = false,
                        StopReason = null,
                        LastResult = ScrapeState.Running,
                        LastMessage = descriptors.Count > 1
                        ? $"Preparing {descriptors.Count} scrapers for parallel execution."
                        : startedMessage,
                        ActiveScrapers = []
                    };

                    runTask = Task.Run(
                        () => RunInternalAsync(
                            request,
                            descriptors,
                            leaseOwnerId,
                            manualCancellationTokenSource,
                            timeoutCancellationTokenSource,
                            executionCancellationTokenSource),
                        CancellationToken.None);

                    _activeRunTask = runTask;
                    status = CreateStatusSnapshot();
                }
            }

            if(releaseLease)
            {
                executionCancellationTokenSource.Dispose();
                timeoutCancellationTokenSource.Dispose();
                manualCancellationTokenSource.Dispose();
                await ReleaseExecutionLeaseSafelyAsync(leaseOwnerId).ConfigureAwait(false);

                string message = request.Force
                ? "A scraper run is already active. Force start is not supported while another run is running."
                : "A scraper run is already active.";

                return new RunPreparationResult(false, message, status, null);
            }

            return new RunPreparationResult(true, startedMessage, status, runTask);
        }

        private async Task RunInternalAsync(
            ScraperRunRequest request,
            IReadOnlyList<ScraperDescriptor> descriptors,
            string leaseOwnerId,
            CancellationTokenSource manualCancellationTokenSource,
            CancellationTokenSource timeoutCancellationTokenSource,
            CancellationTokenSource executionCancellationTokenSource)
        {
            CancellationToken cancellationToken = executionCancellationTokenSource.Token;
            using CancellationTokenSource leaseRenewalCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task leaseRenewalTask = RenewLeaseLoopAsync(
                leaseOwnerId,
                manualCancellationTokenSource,
                leaseRenewalCancellationTokenSource.Token);

            string finalResult = ScrapeState.Completed;
            string finalMessage = "Scraper run completed.";

            try
            {
                if(descriptors.Count == 0)
                {
                    finalResult = ScrapeState.Failed;
                    finalMessage = "No matching scrapers were resolved for execution.";
                    _logger.LogWarning(finalMessage);
                    return;
                }

                int maxConcurrentScrapers = GetMaxConcurrentScrapers(descriptors.Count);
                SemaphoreSlim? concurrencyGate = descriptors.Count > 1 && maxConcurrentScrapers < descriptors.Count
                ? new SemaphoreSlim(maxConcurrentScrapers, maxConcurrentScrapers)
                : null;

                try
                {
                    Task<ScraperExecutionResult>[] tasks = descriptors
                                                           .Select(descriptor => RunScraperWithThrottleAsync(
                                                               descriptor,
                                                               request,
                                                               timeoutCancellationTokenSource,
                                                               manualCancellationTokenSource,
                                                               cancellationToken,
                                                               concurrencyGate))
                                                           .ToArray();

                    ScraperExecutionResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

                    if(results.Any(entry => entry.Cancelled))
                    {
                        bool timedOut = timeoutCancellationTokenSource.IsCancellationRequested &&
                                        !manualCancellationTokenSource.IsCancellationRequested;
                        finalResult = ScrapeState.Cancelled;
                        finalMessage = BuildCancellationMessage(timedOut);
                    }
                    else if(results.Any(entry => entry.Failed))
                    {
                        finalResult = ScrapeState.Failed;
                        finalMessage = "Scraper run completed with at least one failed scraper.";
                    }
                }
                finally
                {
                    concurrencyGate?.Dispose();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                bool timedOut = timeoutCancellationTokenSource.IsCancellationRequested &&
                                !manualCancellationTokenSource.IsCancellationRequested;

                finalResult = ScrapeState.Cancelled;
                finalMessage = BuildCancellationMessage(timedOut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during scraper runtime execution.");
                finalResult = ScrapeState.Failed;
                finalMessage = ex.Message;
            }
            finally
            {
                try
                {
                    leaseRenewalCancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }

                await AwaitBackgroundTaskAsync(leaseRenewalTask, "scraper execution lease renewal").ConfigureAwait(false);
                await ReleaseExecutionLeaseSafelyAsync(leaseOwnerId).ConfigureAwait(false);

                executionCancellationTokenSource.Dispose();
                timeoutCancellationTokenSource.Dispose();

                CompleteRun(finalResult, finalMessage);
            }
        }

        private async Task<ScraperExecutionResult> RunScraperWithThrottleAsync(
            ScraperDescriptor descriptor,
            ScraperRunRequest request,
            CancellationTokenSource timeoutCancellationTokenSource,
            CancellationTokenSource manualCancellationTokenSource,
            CancellationToken cancellationToken,
            SemaphoreSlim? concurrencyGate)
        {
            bool enteredGate = false;

            try
            {
                if(concurrencyGate is not null)
                {
                    await concurrencyGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                    enteredGate = true;
                }

                return await RunScraperAsync(
                    descriptor,
                    request,
                    timeoutCancellationTokenSource,
                    manualCancellationTokenSource,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new ScraperExecutionResult(true, false);
            }
            finally
            {
                if(enteredGate)
                {
                    concurrencyGate!.Release();
                }
            }
        }

        private async Task<ScraperExecutionResult> RunScraperAsync(
            ScraperDescriptor descriptor,
            ScraperRunRequest request,
            CancellationTokenSource timeoutCancellationTokenSource,
            CancellationTokenSource manualCancellationTokenSource,
            CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            IWikiScraper? scraper = ResolveScraper(scope.ServiceProvider, descriptor);

            ScrapeLog log = new()
            {
                StartedAt = DateTime.UtcNow,
                Status = ScrapeState.Running,
                TriggeredBy = request.TriggeredBy,
                Success = false,
                ScraperName = descriptor.ScraperName,
                CategoryName = descriptor.CategoryName,
                CategorySlug = descriptor.CategorySlug
            };

            db.ScrapeLogs.Add(log);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            RegisterActiveScraper(descriptor, log.Id);

            bool countAsCompleted = false;
            bool failed = false;

            try
            {
                if(scraper is null)
                {
                    failed = true;
                    string message = $"Unable to resolve scraper {descriptor.ScraperName} for {descriptor.CategorySlug}.";

                    await FinalizeScrapeLogAsync(
                        log.Id,
                        logEntry =>
                        {
                            logEntry.Success = false;
                            logEntry.Status = ScrapeState.Failed;
                            logEntry.ErrorType = "ScraperResolutionFailed";
                            logEntry.ErrorMessage = message;
                            logEntry.FinishedAt = DateTime.UtcNow;
                        },
                        new ScrapeError
                        {
                            ScrapeLogId = log.Id,
                            Scope = "Runtime",
                            ErrorType = "ScraperResolutionFailed",
                            Message = message,
                            OccurredAt = DateTime.UtcNow
                        },
                        cancellationToken).ConfigureAwait(false);

                    countAsCompleted = true;
                    await _cacheInvalidationService.InvalidateScraperQueriesAsync(log.Id, cancellationToken).ConfigureAwait(false);
                    return new ScraperExecutionResult(false, true);
                }

                _logger.LogInformation(
                    "Starting scraper {ScraperName} for category {CategorySlug}.",
                    scraper.RuntimeScraperName,
                    scraper.RuntimeCategorySlug);

                await scraper.ExecuteAsync(db, log, cancellationToken).ConfigureAwait(false);

                bool logSuccess = log.ItemsFailed == 0 && string.IsNullOrWhiteSpace(log.ErrorMessage);
                countAsCompleted = true;
                failed = !logSuccess;

                await FinalizeScrapeLogAsync(
                    log.Id,
                    logEntry =>
                    {
                        logEntry.Success = logSuccess;
                        logEntry.Status = logSuccess ? ScrapeState.Completed : ScrapeState.Failed;
                        logEntry.FinishedAt = DateTime.UtcNow;
                    },
                    null,
                    cancellationToken).ConfigureAwait(false);

                if(logSuccess)
                {
                    await _cacheInvalidationService.InvalidateScrapedContentAsync(scraper.RuntimeCategorySlug, cancellationToken).ConfigureAwait(false);
                }

                await _cacheInvalidationService.InvalidateScraperQueriesAsync(log.Id, cancellationToken).ConfigureAwait(false);
                return new ScraperExecutionResult(false, failed);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                bool timedOut = timeoutCancellationTokenSource.IsCancellationRequested &&
                                !manualCancellationTokenSource.IsCancellationRequested;
                string cancellationMessage = BuildCancellationMessage(timedOut);

                await FinalizeScrapeLogAsync(
                    log.Id,
                    logEntry =>
                    {
                        logEntry.Success = false;
                        logEntry.Status = ScrapeState.Cancelled;
                        logEntry.ErrorType = nameof(OperationCanceledException);
                        logEntry.ErrorMessage = cancellationMessage;
                        logEntry.FinishedAt = DateTime.UtcNow;
                    },
                    null,
                    CancellationToken.None).ConfigureAwait(false);

                await _cacheInvalidationService.InvalidateScraperQueriesAsync(log.Id, CancellationToken.None).ConfigureAwait(false);

                return new ScraperExecutionResult(true, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scraping in {ScraperName}.", descriptor.ScraperName);

                failed = true;
                await FinalizeScrapeLogAsync(
                    log.Id,
                    logEntry =>
                    {
                        logEntry.Success = false;
                        logEntry.Status = ScrapeState.Failed;
                        logEntry.ErrorType = ex.GetType().Name;
                        logEntry.ErrorMessage = ex.Message;
                        logEntry.FinishedAt = DateTime.UtcNow;
                    },
                    new ScrapeError
                    {
                        ScrapeLogId = log.Id,
                        Scope = "Scraper",
                        ErrorType = ex.GetType().Name,
                        Message = ex.Message,
                        DetailsJson = JsonSerializer.Serialize(new
                        {
                            ex.Message,
                            ex.StackTrace
                        }),
                        OccurredAt = DateTime.UtcNow
                    },
                    cancellationToken).ConfigureAwait(false);

                countAsCompleted = true;
                await _cacheInvalidationService.InvalidateScraperQueriesAsync(log.Id, cancellationToken).ConfigureAwait(false);
                return new ScraperExecutionResult(false, true);
            }
            finally
            {
                MarkScraperFinished(log.Id, countAsCompleted, failed);
            }
        }

        private async Task FinalizeScrapeLogAsync(
            int scrapeLogId,
            Action<ScrapeLog> applyUpdates,
            ScrapeError? additionalError,
            CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            ScrapeLog? log = await db.ScrapeLogs.FirstOrDefaultAsync(entry => entry.Id == scrapeLogId, cancellationToken).ConfigureAwait(false);
            if(log is null)
            {
                return;
            }

            applyUpdates(log);

            if(additionalError is not null)
            {
                db.ScrapeErrors.Add(additionalError);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task RenewLeaseLoopAsync(
            string leaseOwnerId,
            CancellationTokenSource manualCancellationTokenSource,
            CancellationToken cancellationToken)
        {
            int consecutiveFailures = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(GetExecutionLeaseRenewalInterval(), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                bool renewed;

                try
                {
                    renewed = await _scraperExecutionLeaseService.RenewAsync(
                        _runtimeOptions.ExecutionLeaseName,
                        leaseOwnerId,
                        GetExecutionLeaseDuration(),
                        cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to renew scraper execution lease {LeaseName}.", _runtimeOptions.ExecutionLeaseName);
                    renewed = false;
                }

                if(renewed)
                {
                    consecutiveFailures = 0;
                    continue;
                }

                consecutiveFailures++;

                _logger.LogWarning(
                    "Failed to renew scraper execution lease {LeaseName}. Failure {Failure}/{MaxFailures}.",
                    _runtimeOptions.ExecutionLeaseName,
                    consecutiveFailures,
                    GetMaxLeaseRenewalFailures());

                if(consecutiveFailures < GetMaxLeaseRenewalFailures())
                {
                    continue;
                }

                lock (_sync)
                {
                    _state.StopRequested = true;
                    _state.StopReason = "Execution lease was lost.";
                    _state.LastMessage = "Scraper execution lease was lost.";
                }

                try
                {
                    manualCancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }
        }

        private void RegisterActiveScraper(ScraperDescriptor descriptor, int scrapeLogId)
        {
            lock (_sync)
            {
                _state.ActiveScrapers.Add(new ActiveScraperState(
                    scrapeLogId,
                    descriptor.ScraperName,
                    descriptor.CategoryName,
                    descriptor.CategorySlug,
                    DateTime.UtcNow));

                _state.LastMessage = _state.ActiveScrapers.Count == 1
                ? $"Running {descriptor.ScraperName}."
                : $"Running {_state.ActiveScrapers.Count} scraper tasks in parallel.";
            }
        }

        private void MarkScraperFinished(int scrapeLogId, bool countAsCompleted, bool failed)
        {
            lock (_sync)
            {
                _state.ActiveScrapers.RemoveAll(entry => entry.ScrapeLogId == scrapeLogId);

                if(countAsCompleted)
                {
                    _state.CompletedScrapers++;
                }

                if(_state.IsRunning)
                {
                    _state.LastMessage = _state.ActiveScrapers.Count > 0
                    ? $"Running {_state.ActiveScrapers.Count} scraper tasks in parallel."
                    : failed
                    ? "Scraper execution completed with failures."
                    : $"Completed {_state.CompletedScrapers} of {_state.TotalScrapers} scraper tasks.";
                }
            }
        }

        private void CompleteRun(string finalResult, string finalMessage)
        {
            lock (_sync)
            {
                _state.IsRunning = false;
                _state.ActiveScrapers.Clear();
                _state.FinishedAt = DateTime.UtcNow;
                _state.LastResult = finalResult;
                _state.LastMessage = finalMessage;

                _activeRunTask = null;

                _activeRunCancellationTokenSource?.Dispose();
                _activeRunCancellationTokenSource = null;
            }
        }

        private async Task MarkCancellationRequestedAsync(
            IReadOnlyList<int> scrapeLogIds,
            ScraperStopRequest request,
            CancellationToken cancellationToken)
        {
            if(scrapeLogIds.Count == 0)
            {
                return;
            }

            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            List<ScrapeLog> logs = await db.ScrapeLogs
                                           .Where(entry => scrapeLogIds.Contains(entry.Id))
                                           .ToListAsync(cancellationToken).ConfigureAwait(false);

            if(logs.Count == 0)
            {
                return;
            }

            string message = NormalizeOptional(request.Reason) ?? "Stop requested.";

            foreach(ScrapeLog log in logs)
            {
                if(!string.Equals(log.Status, ScrapeState.Running, StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(log.Status, ScrapeState.CancellationRequested, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                log.Status = ScrapeState.CancellationRequested;
                log.ErrorMessage = message;
                log.ErrorType = nameof(OperationCanceledException);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private Task<IReadOnlyList<ScraperDescriptor>> GetMatchingScrapersAsync(ScraperRunRequest request)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IReadOnlyList<IWikiScraper> scrapers = ResolveMatchingScrapers(scope.ServiceProvider, request);

            IReadOnlyList<ScraperDescriptor> descriptors = scrapers
                                                           .Select(scraper => new ScraperDescriptor(
                                                               scraper.RuntimeScraperName,
                                                               scraper.RuntimeCategoryName,
                                                               scraper.RuntimeCategorySlug))
                                                           .Distinct()
                                                           .ToList();

            return Task.FromResult(descriptors);
        }

        private static IReadOnlyList<IWikiScraper> ResolveMatchingScrapers(
            IServiceProvider serviceProvider,
            ScraperRunRequest request)
        {
            IEnumerable<IWikiScraper> scrapers = serviceProvider.GetRequiredService<IEnumerable<IWikiScraper>>();

            if(!string.IsNullOrWhiteSpace(request.ScraperName))
            {
                scrapers = scrapers.Where(scraper =>
                string.Equals(scraper.RuntimeScraperName, request.ScraperName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scraper.GetType().Name, request.ScraperName, StringComparison.OrdinalIgnoreCase));
            }

            if(!string.IsNullOrWhiteSpace(request.CategorySlug))
            {
                scrapers = scrapers.Where(scraper =>
                string.Equals(scraper.RuntimeCategorySlug, request.CategorySlug, StringComparison.OrdinalIgnoreCase));
            }

            return scrapers.ToList();
        }

        private static IWikiScraper? ResolveScraper(IServiceProvider serviceProvider, ScraperDescriptor descriptor)
        {
            return serviceProvider.GetRequiredService<IEnumerable<IWikiScraper>>()
                                  .FirstOrDefault(scraper =>
                                  string.Equals(scraper.RuntimeScraperName, descriptor.ScraperName, StringComparison.OrdinalIgnoreCase) &&
                                  string.Equals(scraper.RuntimeCategorySlug, descriptor.CategorySlug, StringComparison.OrdinalIgnoreCase));
        }

        private static ScraperRunRequest NormalizeRequest(ScraperRunRequest request, string defaultTriggeredBy)
        {
            return request with
            {
                ScraperName = NormalizeOptional(request.ScraperName),
                CategorySlug = NormalizeOptional(request.CategorySlug),
                TriggeredBy = NormalizeOptional(request.TriggeredBy) ?? defaultTriggeredBy
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private string BuildCancellationMessage(bool timedOut)
        {
            string? stopReason;

            lock (_sync)
            {
                stopReason = _state.StopReason;
            }

            if(!string.IsNullOrWhiteSpace(stopReason))
            {
                return $"Scraper run was cancelled: {stopReason}";
            }

            return timedOut
            ? $"Scraper run timed out after {GetRunTimeout().TotalMinutes:0} minutes."
            : "Scraper run was cancelled.";
        }

        private string BuildLeaseConflictMessage(ScraperExecutionLeaseAcquireResult leaseAcquireResult)
        {
            string owner = string.IsNullOrWhiteSpace(leaseAcquireResult.CurrentOwnerId)
            ? "another instance"
            : leaseAcquireResult.CurrentOwnerId;

            return leaseAcquireResult.ExpiresAt.HasValue
            ? $"Another scraper run already holds the execution lease ({owner}) until {leaseAcquireResult.ExpiresAt.Value:u}."
            : $"Another scraper run already holds the execution lease ({owner}).";
        }

        private async Task ReleaseExecutionLeaseSafelyAsync(string leaseOwnerId)
        {
            try
            {
                await _scraperExecutionLeaseService.ReleaseAsync(
                    _runtimeOptions.ExecutionLeaseName,
                    leaseOwnerId,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release scraper execution lease {LeaseName}.", _runtimeOptions.ExecutionLeaseName);
            }
        }

        private async Task AwaitBackgroundTaskAsync(Task task, string taskName)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background task {TaskName} ended with an error.", taskName);
            }
        }

        private ScraperRuntimeStatus CreateStatusSnapshot()
        {
            List<ActiveScraperRuntimeStatus> activeScrapers = _state.ActiveScrapers
                                                                    .OrderBy(entry => entry.CategoryName, StringComparer.OrdinalIgnoreCase)
                                                                    .ThenBy(entry => entry.ScraperName, StringComparer.OrdinalIgnoreCase)
                                                                    .Select(entry => new ActiveScraperRuntimeStatus(
                                                                        entry.ScrapeLogId,
                                                                        entry.ScraperName,
                                                                        entry.CategoryName,
                                                                        entry.CategorySlug,
                                                                        entry.StartedAt))
                                                                    .ToList();

            IReadOnlyList<int> activeScrapeLogIds = activeScrapers
                                                    .Where(entry => entry.ScrapeLogId.HasValue)
                                                    .Select(entry => entry.ScrapeLogId!.Value)
                                                    .ToList();

            ActiveScraperRuntimeStatus? current = activeScrapers.Count == 1 ? activeScrapers[0] : null;
            string? currentScraperName = current?.ScraperName;

            if(currentScraperName is null && activeScrapers.Count > 1)
            {
                currentScraperName = $"{activeScrapers.Count} scrapers active";
            }

            return new ScraperRuntimeStatus(
                _state.IsRunning,
                _state.StopRequested,
                current?.ScrapeLogId,
                activeScrapeLogIds,
                _state.TriggeredBy,
                currentScraperName,
                current?.CategoryName,
                current?.CategorySlug,
                _state.StartedAt,
                _state.FinishedAt,
                _state.TotalScrapers,
                _state.CompletedScrapers,
                activeScrapers.Count,
                activeScrapers,
                _state.LastResult,
                _state.LastMessage,
                _state.StopReason);
        }

        private TimeSpan GetRunTimeout()
        {
            return TimeSpan.FromMinutes(Math.Max(1, _runtimeOptions.RunTimeoutMinutes));
        }

        private TimeSpan GetExecutionLeaseDuration()
        {
            return TimeSpan.FromMinutes(Math.Max(1, _runtimeOptions.ExecutionLeaseDurationMinutes));
        }

        private TimeSpan GetExecutionLeaseRenewalInterval()
        {
            return TimeSpan.FromSeconds(Math.Max(5, _runtimeOptions.ExecutionLeaseRenewalSeconds));
        }

        private int GetMaxLeaseRenewalFailures()
        {
            return Math.Max(1, _runtimeOptions.MaxLeaseRenewalFailures);
        }

        private int GetMaxConcurrentScrapers(int scraperCount)
        {
            int configured = _runtimeOptions.MaxConcurrentScrapers;
            return configured <= 0 ? Math.Max(1, scraperCount) : Math.Clamp(configured, 1, Math.Max(1, scraperCount));
        }

        private static string CreateLeaseOwnerId(ScraperRunRequest request)
        {
            string source = string.IsNullOrWhiteSpace(request.TriggeredBy)
            ? "Unknown"
            : request.TriggeredBy!;

            return $"{Environment.MachineName}:{source}:{Guid.NewGuid():N}";
        }

        private sealed record ScraperDescriptor(
            string ScraperName,
            string CategoryName,
            string CategorySlug);

        private sealed record ScraperExecutionResult(
            bool Cancelled,
            bool Failed);

        private sealed record RunPreparationResult(
            bool Started,
            string Message,
            ScraperRuntimeStatus Status,
            Task? RunTask);

        private sealed record ActiveScraperState(
            int? ScrapeLogId,
            string ScraperName,
            string CategoryName,
            string CategorySlug,
            DateTime StartedAt);

        private sealed class RuntimeState
        {
            public bool IsRunning { get; set; }

            public bool StopRequested { get; set; }

            public string? TriggeredBy { get; set; }

            public DateTime? StartedAt { get; set; }

            public DateTime? FinishedAt { get; set; }

            public int TotalScrapers { get; set; }

            public int CompletedScrapers { get; set; }

            public List<ActiveScraperState> ActiveScrapers { get; set; } = [];

            public string? LastResult { get; set; }

            public string? LastMessage { get; set; }

            public string? StopReason { get; set; }
        }
    }
}