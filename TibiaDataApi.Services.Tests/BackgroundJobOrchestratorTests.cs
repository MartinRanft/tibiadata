using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.BackgroundJobs;
using TibiaDataApi.Services.Entities.Monitoring;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper.Runtime;

namespace TibiaDataApi.Services.Tests
{
    public sealed class BackgroundJobOrchestratorTests
    {
        [Fact]
        public async Task RunAsync_RecordsCompletedExecution_WhenHandlerSucceeds()
        {
            ServiceProvider serviceProvider = CreateServiceProvider();
            BackgroundJobOrchestrator orchestrator = new(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                new StubExecutionLeaseService(),
                NullLogger<BackgroundJobOrchestrator>.Instance);

            BackgroundJobRunResult result = await orchestrator.RunAsync(
                new BackgroundJobDefinition(
                    "test-job",
                    "UnitTest"),
                _ => Task.FromResult(new BackgroundJobExecutionResult(
                    BackgroundJobExecutionState.Completed,
                    "Completed.",
                    3,
                    3)));

            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            BackgroundJobExecution execution = await db.BackgroundJobExecutions.SingleAsync();

            Assert.True(result.Executed);
            Assert.Equal(BackgroundJobExecutionState.Completed, result.Status);
            Assert.Equal("test-job", execution.JobName);
            Assert.Equal(BackgroundJobExecutionState.Completed, execution.Status);
            Assert.Equal(3, execution.ProcessedCount);
            Assert.Equal(3, execution.SucceededCount);
            Assert.NotNull(execution.FinishedAt);
        }

        [Fact]
        public async Task RunAsync_RecordsSkippedExecution_WhenLeaseIsHeld()
        {
            ServiceProvider serviceProvider = CreateServiceProvider();
            BackgroundJobOrchestrator orchestrator = new(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                new StubExecutionLeaseService(new ScraperExecutionLeaseAcquireResult(
                    false,
                    "other-instance",
                    DateTime.UtcNow.AddMinutes(3))),
                NullLogger<BackgroundJobOrchestrator>.Instance);

            bool handlerWasCalled = false;

            BackgroundJobRunResult result = await orchestrator.RunAsync(
                new BackgroundJobDefinition(
                    "leased-job",
                    "UnitTest",
                    "background-job:leased-job",
                    LeaseDurationMinutes: 5,
                    LeaseRenewalSeconds: 30,
                    MaxLeaseRenewalFailures: 3),
                _ =>
                {
                    handlerWasCalled = true;
                    return Task.FromResult(new BackgroundJobExecutionResult(BackgroundJobExecutionState.Completed, "Completed."));
                });

            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            BackgroundJobExecution execution = await db.BackgroundJobExecutions.SingleAsync();

            Assert.False(result.Executed);
            Assert.False(handlerWasCalled);
            Assert.Equal(BackgroundJobExecutionState.Skipped, result.Status);
            Assert.Equal(BackgroundJobExecutionState.Skipped, execution.Status);
            Assert.Equal(1, execution.SkippedCount);
            Assert.NotNull(execution.FinishedAt);
        }

        private static ServiceProvider CreateServiceProvider()
        {
            ServiceCollection services = new();
            InMemoryDatabaseRoot databaseRoot = new();
            string databaseName = Guid.NewGuid().ToString("N");

            services.AddDbContext<TibiaDbContext>(options => { options.UseInMemoryDatabase(databaseName, databaseRoot); });

            return services.BuildServiceProvider();
        }

        private sealed class StubExecutionLeaseService(
            ScraperExecutionLeaseAcquireResult? acquireResult = null) : IScraperExecutionLeaseService
        {
            private readonly ScraperExecutionLeaseAcquireResult _acquireResult =
            acquireResult ?? new ScraperExecutionLeaseAcquireResult(true, "owner", DateTime.UtcNow.AddMinutes(5));

            public Task<ScraperExecutionLeaseAcquireResult> TryAcquireAsync(
                string leaseName,
                string ownerId,
                TimeSpan leaseDuration,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_acquireResult);
            }

            public Task<bool> RenewAsync(
                string leaseName,
                string ownerId,
                TimeSpan leaseDuration,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(true);
            }

            public Task ReleaseAsync(
                string leaseName,
                string ownerId,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}