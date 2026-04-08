namespace TibiaDataApi.Services.Scraper.Runtime
{
    public interface IScraperRuntimeService
    {
        ScraperRuntimeStatus GetStatus();
        Task<ScraperStartResult> StartAsync(
            ScraperRunRequest request,
            CancellationToken cancellationToken = default);
        Task<ScraperStopResult> StopAsync(
            ScraperStopRequest request,
            CancellationToken cancellationToken = default);
        Task<ScraperScheduledRunResult> RunScheduledAsync(CancellationToken cancellationToken = default);
    }
}