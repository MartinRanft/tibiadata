using Microsoft.Extensions.Configuration;

namespace TibiaDataApi.Services.Caching
{
    public static class CachingConfiguration
    {
        public static CachingOptions GetOptions(IConfiguration configuration)
        {
            return configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>() ?? new CachingOptions();
        }

        public static string? GetRedisConnectionString(IConfiguration configuration, CachingOptions options)
        {
            string? connectionString = configuration.GetConnectionString(options.RedisConnectionStringName);
            return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim();
        }
    }
}