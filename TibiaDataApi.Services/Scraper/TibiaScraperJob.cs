using Coravel.Invocable;

using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.BackgroundJobs;
using TibiaDataApi.Services.Scraper.Runtime;

namespace TibiaDataApi.Services.Scraper
{
    public sealed class TibiaScraperJob(
        ILogger<TibiaScraperJob> logger,
        IScraperRuntimeService scraperRuntimeService,
        IBackgroundJobOrchestrator backgroundJobOrchestrator,
        IScheduledScraperConfigurationService scheduledScraperConfigurationService) : IInvocable
    {
        private readonly IBackgroundJobOrchestrator _backgroundJobOrchestrator = backgroundJobOrchestrator;
        private readonly ILogger<TibiaScraperJob> _logger = logger;
        private readonly IScheduledScraperConfigurationService _scheduledScraperConfigurationService = scheduledScraperConfigurationService;
        private readonly IScraperRuntimeService _scraperRuntimeService = scraperRuntimeService;

        public async Task Invoke()
        {
            ScheduledScraperSchedule schedule = await _scheduledScraperConfigurationService.GetAsync().ConfigureAwait(false);

            if(!await _scheduledScraperConfigurationService.ShouldRunAtAsync(DateTime.Now).ConfigureAwait(false))
            {
                return;
            }

            await _backgroundJobOrchestrator.RunAsync(
                new BackgroundJobDefinition(
                    "scheduled-scraper",
                    "Scheduler",
                    TimeoutMinutes: schedule.TimeoutMinutes),
                async cancellationToken =>
                {
                    ScraperScheduledRunResult result = await _scraperRuntimeService.RunScheduledAsync(cancellationToken);

                    if(result.Triggered)
                    {
                        await _scheduledScraperConfigurationService.MarkTriggeredAsync(DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
                    }

                    return new BackgroundJobExecutionResult(
                        result.Triggered ? BackgroundJobExecutionState.Completed : BackgroundJobExecutionState.Skipped,
                        result.Message,
                        Math.Max(0, result.Status.CompletedScrapers),
                        Math.Max(0, result.Status.CompletedScrapers));
                }).ConfigureAwait(false);
        }
    }
}