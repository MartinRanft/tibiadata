using Microsoft.Extensions.Diagnostics.HealthChecks;

using TibiaDataApi.Services.Scraper.Runtime;

namespace TibiaDataApi.Services.HealthChecks
{
    public sealed class ScraperRuntimeHealthCheck(IScraperRuntimeService scraperRuntimeService) : IHealthCheck
    {
        private readonly IScraperRuntimeService _scraperRuntimeService = scraperRuntimeService;

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            ScraperRuntimeStatus status = _scraperRuntimeService.GetStatus();

            Dictionary<string, object> data = new()
            {
                ["isRunning"] = status.IsRunning,
                ["stopRequested"] = status.StopRequested,
                ["totalScrapers"] = status.TotalScrapers,
                ["completedScrapers"] = status.CompletedScrapers
            };

            if(status.ActiveScrapeLogId.HasValue)
            {
                data["activeScrapeLogId"] = status.ActiveScrapeLogId.Value;
            }

            if(!string.IsNullOrWhiteSpace(status.CurrentScraperName))
            {
                data["currentScraperName"] = status.CurrentScraperName;
            }

            if(!string.IsNullOrWhiteSpace(status.CurrentCategoryName))
            {
                data["currentCategoryName"] = status.CurrentCategoryName;
            }

            if(status.StartedAt.HasValue)
            {
                data["startedAt"] = status.StartedAt.Value;
            }

            if(status.FinishedAt.HasValue)
            {
                data["finishedAt"] = status.FinishedAt.Value;
            }

            if(!string.IsNullOrWhiteSpace(status.LastResult))
            {
                data["lastResult"] = status.LastResult;
            }

            if(status.StopRequested)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "A scraper stop has been requested for the active run.",
                    data: data));
            }

            if(status.IsRunning)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    "The scraper runtime is active.",
                    data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "The scraper runtime is idle.",
                data));
        }
    }
}