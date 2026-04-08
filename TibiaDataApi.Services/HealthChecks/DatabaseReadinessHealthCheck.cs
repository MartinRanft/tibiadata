using Microsoft.Extensions.Diagnostics.HealthChecks;

using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.HealthChecks
{
    public sealed class DatabaseReadinessHealthCheck(TibiaDbContext dbContext) : IHealthCheck
    {
        private readonly TibiaDbContext _dbContext = dbContext;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                bool canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
                if(!canConnect)
                {
                    return HealthCheckResult.Unhealthy("Database connection could not be established.");
                }

                return HealthCheckResult.Healthy("Database connection is available.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database health check failed.", ex);
            }
        }
    }
}