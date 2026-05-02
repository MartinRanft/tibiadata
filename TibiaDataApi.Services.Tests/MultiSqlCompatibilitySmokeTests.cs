using Microsoft.EntityFrameworkCore;

using TibiaDataApi.Services.Entities.Monitoring;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class MultiSqlCompatibilitySmokeTests
    {
        [Fact]
        [Trait("Category", "Manual")]
        public async Task MariaDb_ModelCanBeCreated_WhenValidationConnectionIsConfigured()
        {
            string? connectionString = Environment.GetEnvironmentVariable("TIBIADATA_VALIDATION_MARIADB_CONNECTION");

            if(string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            await ValidateProviderAsync(
                connectionString,
                (optionsBuilder, configuredConnectionString) =>
                DatabaseConfiguration.Configure(optionsBuilder, configuredConnectionString, DatabaseProviderNames.MariaDb));
        }

        [Fact]
        [Trait("Category", "Manual")]
        public async Task PostgreSql_ModelCanBeCreated_WhenValidationConnectionIsConfigured()
        {
            string? connectionString = Environment.GetEnvironmentVariable("TIBIADATA_VALIDATION_POSTGRES_CONNECTION");

            if(string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            await ValidateProviderAsync(
                connectionString,
                (optionsBuilder, configuredConnectionString) =>
                DatabaseConfiguration.Configure(optionsBuilder, configuredConnectionString, DatabaseProviderNames.PostgreSql));
        }

        [Fact]
        [Trait("Category", "Manual")]
        public async Task SqlServer_ModelCanBeCreated_WhenValidationConnectionIsConfigured()
        {
            string? connectionString = Environment.GetEnvironmentVariable("TIBIADATA_VALIDATION_SQLSERVER_CONNECTION");

            if(string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            await ValidateProviderAsync(
                connectionString,
                (optionsBuilder, configuredConnectionString) =>
                DatabaseConfiguration.Configure(optionsBuilder, configuredConnectionString, DatabaseProviderNames.SqlServer));
        }

        private static async Task ValidateProviderAsync(
            string connectionString,
            Action<DbContextOptionsBuilder<TibiaDbContext>, string> configure)
        {
            DbContextOptionsBuilder<TibiaDbContext> optionsBuilder = new();
            configure(optionsBuilder, connectionString);

            await using TibiaDbContext dbContext = new(optionsBuilder.Options);
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.ApiRequestLogs.Add(new ApiRequestLog
            {
                IpAddress = "203.0.113.5",
                Method = "GET",
                Route = "/api/v1/items/list",
                StatusCode = 200,
                DurationMs = 12.5,
                UserAgent = "MultiSqlValidation/1.0",
                ResponseSizeBytes = 1024,
                CacheStatus = ApiCacheStatus.Bypass,
                OccurredAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            int logCount = await dbContext.ApiRequestLogs.CountAsync();
            Assert.Equal(1, logCount);
        }
    }
}
