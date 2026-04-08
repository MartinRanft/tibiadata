using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Scraper.Queries
{
    public sealed class ScraperQueryService(
        TibiaDbContext dbContext,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IScraperQueryService
    {
        private readonly HybridCacheEntryOptions _cacheOptions = cachingOptions.ScraperQuery.ToEntryOptions();
        private readonly TibiaDbContext _dbContext = dbContext;
        private readonly HybridCache _hybridCache = hybridCache;

        public async Task<ScraperHistoryPage> GetHistoryAsync(
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            (int normalizedPage, int normalizedPageSize) = NormalizePagination(page, pageSize, 200);

            IQueryable<ScrapeLog> query = _dbContext.ScrapeLogs
                                                    .AsNoTracking()
                                                    .OrderByDescending(entry => entry.StartedAt)
                                                    .ThenByDescending(entry => entry.Id);

            return await _hybridCache.GetOrCreateAsync(
                $"scraper-history:{normalizedPage}:{normalizedPageSize}",
                async cancellationToken =>
                {
                    int totalCount = await query.CountAsync(cancellationToken);

                    List<ScraperHistoryEntry> items = await query
                                                            .Skip((normalizedPage - 1) * normalizedPageSize)
                                                            .Take(normalizedPageSize)
                                                            .Select(entry => new ScraperHistoryEntry(
                                                                entry.Id,
                                                                entry.Status,
                                                                entry.Success,
                                                                entry.ScraperName,
                                                                entry.CategoryName,
                                                                entry.StartedAt,
                                                                entry.FinishedAt,
                                                                entry.ItemsProcessed,
                                                                entry.ItemsAdded,
                                                                entry.ItemsUpdated,
                                                                entry.ItemsUnchanged,
                                                                entry.ItemsFailed,
                                                                entry.ItemsMissingFromSource,
                                                                entry.ErrorType,
                                                                entry.ErrorMessage))
                                                            .ToListAsync(cancellationToken);

                    return new ScraperHistoryPage(normalizedPage, normalizedPageSize, totalCount, items);
                },
                _cacheOptions,
                [CacheTags.ScraperQueries],
                cancellationToken);
        }

        public async Task<ScraperChangesPage> GetChangesAsync(
            int? scrapeLogId = null,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            (int normalizedPage, int normalizedPageSize) = NormalizePagination(page, pageSize, 500);

            IQueryable<ScrapeItemChange> query = _dbContext.ScrapeItemChanges
                                                           .AsNoTracking()
                                                           .OrderByDescending(entry => entry.OccurredAt)
                                                           .ThenByDescending(entry => entry.Id);

            if(scrapeLogId.HasValue)
            {
                query = query.Where(entry => entry.ScrapeLogId == scrapeLogId.Value);
            }

            List<string> tags =
            [
                CacheTags.ScraperQueries,
                scrapeLogId.HasValue ? CacheTags.ScrapeLog(scrapeLogId.Value) : CacheTags.ScraperQueries
            ];

            return await _hybridCache.GetOrCreateAsync(
                $"scraper-changes:{scrapeLogId?.ToString() ?? "all"}:{normalizedPage}:{normalizedPageSize}",
                async cancellationToken =>
                {
                    int totalCount = await query.CountAsync(cancellationToken);

                    List<ScraperChangeEntry> items = await query
                                                           .Skip((normalizedPage - 1) * normalizedPageSize)
                                                           .Take(normalizedPageSize)
                                                           .Select(entry => new ScraperChangeEntry(
                                                               entry.Id,
                                                               entry.ScrapeLogId,
                                                               entry.ChangeType,
                                                               entry.ItemName,
                                                               entry.CategoryName,
                                                               entry.OccurredAt,
                                                               entry.ChangedFieldsJson,
                                                               entry.ErrorMessage))
                                                           .ToListAsync(cancellationToken);

                    return new ScraperChangesPage(normalizedPage, normalizedPageSize, totalCount, items);
                },
                _cacheOptions,
                tags,
                cancellationToken);
        }

        public async Task<ScraperErrorsPage> GetErrorsAsync(
            int? scrapeLogId = null,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            (int normalizedPage, int normalizedPageSize) = NormalizePagination(page, pageSize, 500);

            IQueryable<ScrapeError> query = _dbContext.ScrapeErrors
                                                      .AsNoTracking()
                                                      .OrderByDescending(entry => entry.OccurredAt)
                                                      .ThenByDescending(entry => entry.Id);

            if(scrapeLogId.HasValue)
            {
                query = query.Where(entry => entry.ScrapeLogId == scrapeLogId.Value);
            }

            List<string> tags =
            [
                CacheTags.ScraperQueries,
                scrapeLogId.HasValue ? CacheTags.ScrapeLog(scrapeLogId.Value) : CacheTags.ScraperQueries
            ];

            return await _hybridCache.GetOrCreateAsync(
                $"scraper-errors:{scrapeLogId?.ToString() ?? "all"}:{normalizedPage}:{normalizedPageSize}",
                async cancellationToken =>
                {
                    int totalCount = await query.CountAsync(cancellationToken);

                    List<ScraperErrorEntry> items = await query
                                                          .Skip((normalizedPage - 1) * normalizedPageSize)
                                                          .Take(normalizedPageSize)
                                                          .Select(entry => new ScraperErrorEntry(
                                                              entry.Id,
                                                              entry.ScrapeLogId,
                                                              entry.Scope,
                                                              entry.ErrorType,
                                                              entry.Message,
                                                              entry.PageTitle,
                                                              entry.ItemName,
                                                              entry.OccurredAt))
                                                          .ToListAsync(cancellationToken);

                    return new ScraperErrorsPage(normalizedPage, normalizedPageSize, totalCount, items);
                },
                _cacheOptions,
                tags,
                cancellationToken);
        }

        private static (int Page, int PageSize) NormalizePagination(int page, int pageSize, int maxPageSize)
        {
            int normalizedPage = page < 1 ? 1 : page;
            int normalizedPageSize = pageSize < 1 ? 1 : pageSize;

            if(normalizedPageSize > maxPageSize)
            {
                normalizedPageSize = maxPageSize;
            }

            return (normalizedPage, normalizedPageSize);
        }
    }
}