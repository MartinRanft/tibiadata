using Microsoft.Extensions.Caching.Hybrid;

namespace TibiaDataApi.Services.Caching
{
    public sealed class CacheInvalidationService(HybridCache hybridCache) : ICacheInvalidationService
    {
        private readonly HybridCache _hybridCache = hybridCache;

        public async Task InvalidateIpBansAsync(string? ipAddress = null, CancellationToken cancellationToken = default)
        {
            if(!string.IsNullOrWhiteSpace(ipAddress))
            {
                await _hybridCache.RemoveByTagAsync(CacheTags.IpBanAddress(ipAddress.Trim()), cancellationToken);
            }

            await _hybridCache.RemoveByTagAsync(CacheTags.IpBans, cancellationToken);
        }

        public async Task InvalidateScraperQueriesAsync(int? scrapeLogId = null, CancellationToken cancellationToken = default)
        {
            if(scrapeLogId.HasValue)
            {
                await _hybridCache.RemoveByTagAsync(CacheTags.ScrapeLog(scrapeLogId.Value), cancellationToken);
            }

            await _hybridCache.RemoveByTagAsync(CacheTags.ScraperQueries, cancellationToken);
        }

        public async Task InvalidateScrapedContentAsync(string? categorySlug = null, CancellationToken cancellationToken = default)
        {
            foreach(string cacheTag in CacheTags.ScrapedContentTags)
            {
                await _hybridCache.RemoveByTagAsync(cacheTag, cancellationToken);
            }

            if(!string.IsNullOrWhiteSpace(categorySlug))
            {
                await _hybridCache.RemoveByTagAsync(CacheTags.Category(categorySlug), cancellationToken);
            }
        }
    }
}