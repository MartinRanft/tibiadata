using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using TibiaDataApi.Services.Caching;

namespace TibiaDataApi.Services.HealthChecks
{
    public sealed class RedisReadinessHealthCheck(
        CachingOptions cachingOptions,
        IServiceProvider serviceProvider) : IHealthCheck
    {
        private readonly CachingOptions _cachingOptions = cachingOptions;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if(!_cachingOptions.UseRedisForHybridCache && !_cachingOptions.UseRedisForOutputCache)
            {
                return HealthCheckResult.Healthy("Redis caching is disabled.");
            }

            IDistributedCache? distributedCache = _serviceProvider.GetService<IDistributedCache>();
            if(distributedCache is null)
            {
                return HealthCheckResult.Unhealthy("Redis is configured, but no distributed cache service is registered.");
            }

            string cacheKey = ("health:redis:ping").ToLowerInvariant();
            byte[] payload = [1];

            try
            {
                await distributedCache.SetAsync(
                    cacheKey,
                    payload,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15)
                    },
                    cancellationToken);

                byte[]? cachedValue = await distributedCache.GetAsync(cacheKey, cancellationToken);
                await distributedCache.RemoveAsync(cacheKey, cancellationToken);

                return cachedValue is not null && cachedValue.Length == payload.Length && cachedValue[0] == payload[0]
                ? HealthCheckResult.Healthy("Redis cache is available.")
                : HealthCheckResult.Unhealthy("Redis cache did not return the expected probe value.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Redis health check failed.", ex);
            }
        }
    }
}