namespace TibiaDataApi.Services.BackgroundJobs
{
    public interface IBackgroundJobOrchestrator
    {
        Task<BackgroundJobRunResult> RunAsync(
            BackgroundJobDefinition definition,
            Func<CancellationToken, Task<BackgroundJobExecutionResult>> handler,
            CancellationToken cancellationToken = default);
    }
}