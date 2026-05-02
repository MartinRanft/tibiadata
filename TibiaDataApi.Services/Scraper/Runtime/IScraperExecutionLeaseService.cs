namespace TibiaDataApi.Services.Scraper.Runtime
{
    public interface IScraperExecutionLeaseService
    {
        Task<ScraperExecutionLeaseAcquireResult> TryAcquireAsync(
            string leaseName,
            string ownerId,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default);

        Task<bool> RenewAsync(
            string leaseName,
            string ownerId,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default);

        Task ReleaseAsync(
            string leaseName,
            string ownerId,
            CancellationToken cancellationToken = default);
    }
}