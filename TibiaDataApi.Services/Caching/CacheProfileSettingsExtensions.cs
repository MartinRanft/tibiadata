using Microsoft.Extensions.Caching.Hybrid;

namespace TibiaDataApi.Services.Caching
{
    internal static class CacheProfileSettingsExtensions
    {
        public static HybridCacheEntryOptions ToEntryOptions(this CacheProfileSettings settings)
        {
            return new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(Math.Max(1, settings.ExpirationSeconds)),
                LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, settings.LocalExpirationSeconds))
            };
        }
    }
}