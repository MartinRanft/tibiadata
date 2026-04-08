using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Entities.Monitoring;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Admin.Statistics
{
    public sealed class ApiStatisticsService(
        TibiaDbContext dbContext,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IApiStatisticsService
    {
        private readonly HybridCacheEntryOptions _cacheOptions = cachingOptions.ApiStatistics.ToEntryOptions();
        private readonly TibiaDbContext _dbContext = dbContext;
        private readonly HybridCache _hybridCache = hybridCache;

        public async Task RecordRequestAsync(
            ApiRequestRecord request,
            CancellationToken cancellationToken = default)
        {
            if(!ShouldIncludeInAnalytics(request.IpAddress, request.Route))
            {
                return;
            }

            ApiRequestLog entry = new()
            {
                IpAddress = Truncate(request.IpAddress, 64),
                Method = Truncate(request.Method, 16),
                Route = Truncate(request.Route, 500),
                StatusCode = request.StatusCode,
                DurationMs = request.DurationMs,
                UserAgent = TruncateOptional(request.UserAgent, 512),
                ResponseSizeBytes = Math.Max(0, request.ResponseSizeBytes),
                CacheStatus = NormalizeCacheStatus(request.CacheStatus),
                IsBlocked = request.IsBlocked,
                OccurredAt = request.OccurredAt
            };

            _dbContext.ApiRequestLogs.Add(entry);
            await UpdateDailyAggregateAsync(request, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<ApiStatisticsSummary> GetSummaryAsync(
            int days = 30,
            CancellationToken cancellationToken = default)
        {
            int normalizedDays = Math.Clamp(days, 1, 90);
            DateTime startDateUtc = DateTime.UtcNow.Date.AddDays(-(normalizedDays - 1));

            return await _hybridCache.GetOrCreateAsync(
                $"api-statistics:{normalizedDays}",
                async cancellationToken =>
                {
                    List<ApiRequestProjection> requests = await _dbContext.ApiRequestLogs
                                                                          .AsNoTracking()
                                                                          .Where(entry => entry.OccurredAt >= startDateUtc)
                                                                          .Select(entry => new ApiRequestProjection(
                                                                              entry.IpAddress,
                                                                              entry.Method,
                                                                              entry.Route,
                                                                              entry.StatusCode,
                                                                              entry.DurationMs,
                                                                              entry.ResponseSizeBytes,
                                                                              entry.CacheStatus,
                                                                              entry.IsBlocked,
                                                                              entry.OccurredAt))
                                                                          .ToListAsync(cancellationToken);

                    List<ApiRequestDailyAggregate> dailyAggregates = await _dbContext.ApiRequestDailyAggregates
                                                                                      .AsNoTracking()
                                                                                      .Where(entry => entry.DayUtc >= startDateUtc)
                                                                                      .OrderBy(entry => entry.DayUtc)
                                                                                      .ToListAsync(cancellationToken);

                    requests = requests
                               .Where(entry => ShouldIncludeInAnalytics(entry.IpAddress, entry.Route))
                               .ToList();

                    int totalRequests = requests.Count;
                    int uniqueIpCount = requests
                                        .Select(entry => entry.IpAddress)
                                        .Where(entry => !string.IsNullOrWhiteSpace(entry))
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .Count();

                    int errorCount = requests.Count(entry => entry.StatusCode >= 400);
                    int blockedCount = requests.Count(entry => entry.IsBlocked);
                    long totalResponseSizeBytes = requests.Sum(entry => entry.ResponseSizeBytes);
                    double averageResponseTimeMs = totalRequests == 0
                    ? 0
                    : requests.Average(entry => entry.DurationMs);
                    double averageResponseSizeBytes = totalRequests == 0
                    ? 0
                    : requests.Average(entry => entry.ResponseSizeBytes);

                    int cacheHitCount = requests.Count(entry => string.Equals(entry.CacheStatus, ApiCacheStatus.Hit, StringComparison.Ordinal));
                    int cacheMissCount = requests.Count(entry => string.Equals(entry.CacheStatus, ApiCacheStatus.Miss, StringComparison.Ordinal));
                    int cacheBypassCount = requests.Count(entry => string.Equals(entry.CacheStatus, ApiCacheStatus.Bypass, StringComparison.Ordinal));

                    DateTime? peakRequestHourUtc = requests.Count == 0
                    ? null
                    : requests.GroupBy(entry => new DateTime(
                                        entry.OccurredAt.Year,
                                        entry.OccurredAt.Month,
                                        entry.OccurredAt.Day,
                                        entry.OccurredAt.Hour,
                                        0,
                                        0,
                                        DateTimeKind.Utc))
                              .OrderByDescending(group => group.Count())
                              .ThenByDescending(group => group.Key)
                              .Select(group => group.Key)
                              .FirstOrDefault();

                    List<ApiDailyStatBucket> daily = BuildDailyBuckets(requests, dailyAggregates, startDateUtc, normalizedDays);

                    List<ApiEndpointStat> topEndpoints = requests
                                                         .GroupBy(entry => new
                                                         {
                                                             Route = string.IsNullOrWhiteSpace(entry.Route) ? "/unknown" : entry.Route,
                                                             Method = string.IsNullOrWhiteSpace(entry.Method) ? "UNKNOWN" : entry.Method
                                                         })
                                                         .Select(group => new ApiEndpointStat(
                                                             group.Key.Route,
                                                             group.Key.Method,
                                                             group.Count(),
                                                             group.Average(entry => entry.DurationMs)))
                                                         .OrderByDescending(entry => entry.RequestCount)
                                                         .ThenBy(entry => entry.Route, StringComparer.OrdinalIgnoreCase)
                                                         .ThenBy(entry => entry.Method, StringComparer.OrdinalIgnoreCase)
                                                         .Take(20)
                                                         .ToList();

                    List<ApiStatusCodeStat> topStatusCodes = requests
                                                             .GroupBy(entry => entry.StatusCode)
                                                             .Select(group => new ApiStatusCodeStat(group.Key, group.Count()))
                                                             .OrderByDescending(entry => entry.RequestCount)
                                                             .ThenBy(entry => entry.StatusCode)
                                                             .Take(10)
                                                             .ToList();

                    List<ApiIpStat> topIps = requests
                                             .GroupBy(entry => string.IsNullOrWhiteSpace(entry.IpAddress) ? "unknown" : entry.IpAddress)
                                             .Select(group => new ApiIpStat(
                                                 group.Key,
                                                 group.Count(),
                                                 group.Count(entry => entry.IsBlocked)))
                                             .OrderByDescending(entry => entry.RequestCount)
                                             .ThenBy(entry => entry.IpAddress, StringComparer.OrdinalIgnoreCase)
                                             .Take(20)
                                             .ToList();

                    return new ApiStatisticsSummary(
                        normalizedDays,
                        totalRequests,
                        uniqueIpCount,
                        errorCount,
                        blockedCount,
                        averageResponseTimeMs,
                        totalResponseSizeBytes,
                        averageResponseSizeBytes,
                        cacheHitCount,
                        cacheMissCount,
                        cacheBypassCount,
                        peakRequestHourUtc,
                        daily,
                        topEndpoints,
                        topStatusCodes,
                        topIps);
                },
                _cacheOptions,
                [CacheTags.ApiStatistics],
                cancellationToken);
        }

        public async Task<ApiRequestLogPage> GetRequestLogsAsync(
            int days = 1,
            string? ipAddress = null,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            int normalizedDays = Math.Clamp(days, 1, 90);
            string? normalizedIpAddress = NormalizeIpAddress(ipAddress);
            (int normalizedPage, int normalizedPageSize) = NormalizePagination(page, pageSize, 500);
            DateTime startDateUtc = DateTime.UtcNow.AddDays(-normalizedDays);

            List<ApiRequestLogEntry> allItems = await _dbContext.ApiRequestLogs
                                                                .AsNoTracking()
                                                                .Where(entry => entry.OccurredAt >= startDateUtc)
                                                                .OrderByDescending(entry => entry.OccurredAt)
                                                                .ThenByDescending(entry => entry.Id)
                                                                .Select(entry => new ApiRequestLogEntry(
                                                                    entry.Id,
                                                                    entry.IpAddress,
                                                                    entry.Method,
                                                                    entry.Route,
                                                                    entry.StatusCode,
                                                                    entry.DurationMs,
                                                                    entry.UserAgent,
                                                                    entry.ResponseSizeBytes,
                                                                    entry.CacheStatus,
                                                                    entry.IsBlocked,
                                                                    entry.OccurredAt))
                                                                .ToListAsync(cancellationToken);

            List<ApiRequestLogEntry> filteredItems = allItems
                                                     .Where(entry => ShouldIncludeInAnalytics(entry.IpAddress, entry.Route))
                                                     .Where(entry =>
                                                     string.IsNullOrWhiteSpace(normalizedIpAddress) ||
                                                     string.Equals(entry.IpAddress, normalizedIpAddress, StringComparison.OrdinalIgnoreCase))
                                                     .ToList();

            int totalCount = filteredItems.Count;
            List<ApiRequestLogEntry> items = filteredItems
                                             .Skip((normalizedPage - 1) * normalizedPageSize)
                                             .Take(normalizedPageSize)
                                             .ToList();

            return new ApiRequestLogPage(
                normalizedDays,
                normalizedIpAddress,
                normalizedPage,
                normalizedPageSize,
                totalCount,
                items);
        }

        public async Task<ApiIpActivityDetails> GetIpActivityAsync(
            string ipAddress,
            int days = 1,
            int recentRequestCount = 50,
            CancellationToken cancellationToken = default)
        {
            int normalizedDays = Math.Clamp(days, 1, 90);
            string normalizedIpAddress = NormalizeIpAddress(ipAddress) ?? ipAddress.Trim();
            int normalizedRecentRequestCount = Math.Clamp(recentRequestCount, 1, 200);
            DateTime startDateUtc = DateTime.UtcNow.AddDays(-normalizedDays);

            List<ApiRequestLogEntry> recentRequests = await _dbContext.ApiRequestLogs
                                                                      .AsNoTracking()
                                                                      .Where(entry =>
                                                                      entry.OccurredAt >= startDateUtc &&
                                                                      entry.IpAddress == normalizedIpAddress)
                                                                      .OrderByDescending(entry => entry.OccurredAt)
                                                                      .ThenByDescending(entry => entry.Id)
                                                                      .Select(entry => new ApiRequestLogEntry(
                                                                          entry.Id,
                                                                          entry.IpAddress,
                                                                          entry.Method,
                                                                          entry.Route,
                                                                          entry.StatusCode,
                                                                          entry.DurationMs,
                                                                          entry.UserAgent,
                                                                          entry.ResponseSizeBytes,
                                                                          entry.CacheStatus,
                                                                          entry.IsBlocked,
                                                                          entry.OccurredAt))
                                                                      .ToListAsync(cancellationToken);

            recentRequests = recentRequests
                             .Where(entry => ShouldIncludeInAnalytics(entry.IpAddress, entry.Route))
                             .Take(normalizedRecentRequestCount)
                             .ToList();

            List<ApiRequestProjection> requests = await _dbContext.ApiRequestLogs
                                                                  .AsNoTracking()
                                                                  .Where(entry =>
                                                                  entry.OccurredAt >= startDateUtc &&
                                                                  entry.IpAddress == normalizedIpAddress)
                                                                  .Select(entry => new ApiRequestProjection(
                                                                      entry.IpAddress,
                                                                      entry.Method,
                                                                      entry.Route,
                                                                      entry.StatusCode,
                                                                      entry.DurationMs,
                                                                      entry.ResponseSizeBytes,
                                                                      entry.CacheStatus,
                                                                      entry.IsBlocked,
                                                                      entry.OccurredAt))
                                                                  .ToListAsync(cancellationToken);

            requests = requests
                       .Where(entry => ShouldIncludeInAnalytics(entry.IpAddress, entry.Route))
                       .ToList();

            int totalRequests = requests.Count;
            int blockedCount = requests.Count(entry => entry.IsBlocked);
            int errorCount = requests.Count(entry => entry.StatusCode >= 400);
            long totalResponseSizeBytes = requests.Sum(entry => entry.ResponseSizeBytes);
            double averageResponseTimeMs = totalRequests == 0
            ? 0
            : requests.Average(entry => entry.DurationMs);
            double averageResponseSizeBytes = totalRequests == 0
            ? 0
            : requests.Average(entry => entry.ResponseSizeBytes);

            List<ApiEndpointStat> topEndpoints = requests
                                                 .GroupBy(entry => new
                                                 {
                                                     Route = string.IsNullOrWhiteSpace(entry.Route) ? "/unknown" : entry.Route,
                                                     Method = string.IsNullOrWhiteSpace(entry.Method) ? "UNKNOWN" : entry.Method
                                                 })
                                                 .Select(group => new ApiEndpointStat(
                                                     group.Key.Route,
                                                     group.Key.Method,
                                                     group.Count(),
                                                     group.Average(entry => entry.DurationMs)))
                                                 .OrderByDescending(entry => entry.RequestCount)
                                                 .ThenBy(entry => entry.Route, StringComparer.OrdinalIgnoreCase)
                                                 .ThenBy(entry => entry.Method, StringComparer.OrdinalIgnoreCase)
                                                 .Take(10)
                                                 .ToList();

            DateTime? firstSeenAt = requests.Count == 0 ? null : requests.Min(entry => entry.OccurredAt);
            DateTime? lastSeenAt = requests.Count == 0 ? null : requests.Max(entry => entry.OccurredAt);

            return new ApiIpActivityDetails(
                normalizedIpAddress,
                normalizedDays,
                totalRequests,
                blockedCount,
                errorCount,
                averageResponseTimeMs,
                totalResponseSizeBytes,
                averageResponseSizeBytes,
                firstSeenAt,
                lastSeenAt,
                topEndpoints,
                recentRequests);
        }

        private static List<ApiDailyStatBucket> BuildDailyBuckets(
            IReadOnlyCollection<ApiRequestProjection> requests,
            IReadOnlyCollection<ApiRequestDailyAggregate> dailyAggregates,
            DateTime startDateUtc,
            int days)
        {
            Dictionary<DateOnly, ApiRequestProjection[]> requestGroups = requests
                                                                         .GroupBy(entry => DateOnly.FromDateTime(entry.OccurredAt))
                                                                         .ToDictionary(
                                                                             group => group.Key,
                                                                             group => group.ToArray());

            Dictionary<DateOnly, ApiRequestDailyAggregate> aggregateByDay = dailyAggregates
                                                                            .ToDictionary(
                                                                                entry => DateOnly.FromDateTime(entry.DayUtc),
                                                                                entry => entry);

            List<ApiDailyStatBucket> buckets = new(days);

            for (int index = 0; index < days; index++)
            {
                DateOnly day = DateOnly.FromDateTime(startDateUtc.AddDays(index));

                if(aggregateByDay.TryGetValue(day, out ApiRequestDailyAggregate? aggregate))
                {
                    buckets.Add(new ApiDailyStatBucket(
                        day,
                        aggregate.RequestCount,
                        aggregate.ErrorCount,
                        aggregate.BlockedCount,
                        aggregate.CacheHitCount,
                        aggregate.CacheMissCount,
                        aggregate.CacheBypassCount,
                        aggregate.TotalResponseSizeBytes,
                        aggregate.RequestCount == 0 ? 0 : aggregate.TotalDurationMs / aggregate.RequestCount));
                    continue;
                }

                if(!requestGroups.TryGetValue(day, out ApiRequestProjection[]? dayEntries))
                {
                    buckets.Add(new ApiDailyStatBucket(day, 0, 0, 0, 0, 0, 0, 0, 0));
                    continue;
                }

                buckets.Add(new ApiDailyStatBucket(
                    day,
                    dayEntries.Length,
                    dayEntries.Count(entry => entry.StatusCode >= 400),
                    dayEntries.Count(entry => entry.IsBlocked),
                    dayEntries.Count(entry => string.Equals(entry.CacheStatus, ApiCacheStatus.Hit, StringComparison.Ordinal)),
                    dayEntries.Count(entry => string.Equals(entry.CacheStatus, ApiCacheStatus.Miss, StringComparison.Ordinal)),
                    dayEntries.Count(entry => string.Equals(entry.CacheStatus, ApiCacheStatus.Bypass, StringComparison.Ordinal)),
                    dayEntries.Sum(entry => entry.ResponseSizeBytes),
                    dayEntries.Length == 0 ? 0 : dayEntries.Average(entry => entry.DurationMs)));
            }

            return buckets;
        }

        private async Task UpdateDailyAggregateAsync(ApiRequestRecord request, CancellationToken cancellationToken)
        {
            DateTime dayUtc = request.OccurredAt.Date;
            ApiRequestDailyAggregate? aggregate = await _dbContext.ApiRequestDailyAggregates
                                                                  .SingleOrDefaultAsync(entry => entry.DayUtc == dayUtc, cancellationToken);

            if(aggregate is null)
            {
                aggregate = new ApiRequestDailyAggregate
                {
                    DayUtc = dayUtc
                };

                _dbContext.ApiRequestDailyAggregates.Add(aggregate);
            }

            aggregate.RequestCount += 1;
            aggregate.ErrorCount += request.StatusCode >= 400 ? 1 : 0;
            aggregate.BlockedCount += request.IsBlocked ? 1 : 0;
            aggregate.TotalResponseSizeBytes += Math.Max(0, request.ResponseSizeBytes);
            aggregate.TotalDurationMs += Math.Max(0, request.DurationMs);
            aggregate.UpdatedAt = DateTime.UtcNow;

            switch (NormalizeCacheStatus(request.CacheStatus))
            {
                case ApiCacheStatus.Hit:
                    aggregate.CacheHitCount += 1;
                    break;
                case ApiCacheStatus.Miss:
                    aggregate.CacheMissCount += 1;
                    break;
                default:
                    aggregate.CacheBypassCount += 1;
                    break;
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if(value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }

        private static string? TruncateOptional(string? value, int maxLength)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        private static string NormalizeCacheStatus(string? cacheStatus)
        {
            if(string.Equals(cacheStatus, ApiCacheStatus.Hit, StringComparison.OrdinalIgnoreCase))
            {
                return ApiCacheStatus.Hit;
            }

            if(string.Equals(cacheStatus, ApiCacheStatus.Miss, StringComparison.OrdinalIgnoreCase))
            {
                return ApiCacheStatus.Miss;
            }

            return ApiCacheStatus.Bypass;
        }

        private static bool ShouldIncludeInAnalytics(string? ipAddress, string? route)
        {
            return !IsAdministrativeRoute(route) && !IsLoopbackAddress(ipAddress);
        }

        private static bool IsAdministrativeRoute(string? route)
        {
            if(string.IsNullOrWhiteSpace(route))
            {
                return false;
            }

            return route.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase) ||
                   route.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(route, "/api/scraper/quick-test", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(route, "/openapi/admin.json", StringComparison.OrdinalIgnoreCase) ||
                   route.StartsWith("/scalar/admin", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLoopbackAddress(string? ipAddress)
        {
            if(string.IsNullOrWhiteSpace(ipAddress))
            {
                return false;
            }

            if(string.Equals(ipAddress.Trim(), "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if(!IPAddress.TryParse(ipAddress.Trim(), out IPAddress? parsed))
            {
                return false;
            }

            return IPAddress.IsLoopback(parsed) ||
                   (parsed.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(parsed.MapToIPv4()));
        }

        private static string? NormalizeIpAddress(string? ipAddress)
        {
            return string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
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

        private sealed record ApiRequestProjection(
            string IpAddress,
            string Method,
            string Route,
            int StatusCode,
            double DurationMs,
            long ResponseSizeBytes,
            string CacheStatus,
            bool IsBlocked,
            DateTime OccurredAt);
    }
}
