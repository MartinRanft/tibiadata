using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using TibiaDataApi.AdminAccess;
using TibiaDataApi.Contracts.Admin;
using TibiaDataApi.RequestProtection;
using TibiaDataApi.Services.Admin.Security;
using TibiaDataApi.Services.Admin.Statistics;
using TibiaDataApi.Services.BackgroundJobs;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Scraper;
using TibiaDataApi.Services.Scraper.Queries;
using TibiaDataApi.Services.Scraper.Runtime;

namespace TibiaDataApi.Controller.Admin
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = AdminAccessDefaults.PolicyName)]
    [ApiExplorerSettings(GroupName = AdminAccessDefaults.AdminDocumentName)]
    public sealed class AdminController(
        IScraperRuntimeService scraperRuntimeService,
        IScraperQueryService scraperQueryService,
        IAdminMetricsService adminMetricsService,
        IDatabaseLoadMonitor databaseLoadMonitor,
        IApiStatisticsService apiStatisticsService,
        IIpBanService ipBanService,
        IRequestProtectionConfigurationService requestProtectionConfigurationService,
        IScheduledScraperConfigurationService scheduledScraperConfigurationService,
        IEnumerable<IWikiScraper> availableScrapers,
        HealthCheckService healthCheckService,
        CachingOptions cachingOptions) : ControllerBase
    {
        [HttpGet("scraper/status")]
        [EndpointSummary("Returns the current scraper status")]
        [ProducesResponseType(typeof(AdminScraperStatusResponse), StatusCodes.Status200OK)]
        public ActionResult<AdminScraperStatusResponse> GetScraperStatus()
        {
            ScraperRuntimeStatus status = scraperRuntimeService.GetStatus();
            return Ok(MapStatusResponse(status));
        }

        [HttpGet("scrapers")]
        [EndpointSummary("Returns the registered scrapers that can be started manually")]
        [ProducesResponseType(typeof(AdminScraperCatalogResponse), StatusCodes.Status200OK)]
        public ActionResult<AdminScraperCatalogResponse> GetAvailableScrapers()
        {
            List<AdminScraperCatalogItem> items = availableScrapers
                                                  .Select(scraper => new AdminScraperCatalogItem(
                                                      scraper.RuntimeScraperName,
                                                      scraper.RuntimeCategoryName,
                                                      scraper.RuntimeCategorySlug))
                                                  .Distinct()
                                                  .OrderBy(entry => entry.CategoryName, StringComparer.OrdinalIgnoreCase)
                                                  .ThenBy(entry => entry.ScraperName, StringComparer.OrdinalIgnoreCase)
                                                  .ToList();

            return Ok(new AdminScraperCatalogResponse(items));
        }

        [HttpGet("scraper/history")]
        [EndpointSummary("Returns the scraper run history")]
        [ProducesResponseType(typeof(AdminScraperHistoryResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminScraperHistoryResponse>> GetScraperHistory(
            [FromQuery]int page = 1,
            [FromQuery]int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            ScraperHistoryPage result = await scraperQueryService.GetHistoryAsync(page, pageSize, cancellationToken);

            return Ok(new AdminScraperHistoryResponse(
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.Items
                      .Select(entry => new AdminScraperHistoryItem(
                          entry.ScrapeLogId,
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
                      .ToList()));
        }

        [HttpGet("scraper/changes")]
        [EndpointSummary("Returns the latest detected item changes")]
        [ProducesResponseType(typeof(AdminScraperChangesResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminScraperChangesResponse>> GetScraperChanges(
            [FromQuery]int? scrapeLogId = null,
            [FromQuery]int page = 1,
            [FromQuery]int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            ScraperChangesPage result = await scraperQueryService.GetChangesAsync(
                scrapeLogId,
                page,
                pageSize,
                cancellationToken);

            return Ok(new AdminScraperChangesResponse(
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.Items
                      .Select(entry => new AdminScraperChangeItem(
                          entry.ChangeId,
                          entry.ScrapeLogId,
                          entry.ChangeType,
                          entry.ItemName,
                          entry.CategoryName,
                          entry.OccurredAt,
                          entry.ChangedFieldsJson,
                          entry.ErrorMessage))
                      .ToList()));
        }

        [HttpGet("scraper/errors")]
        [EndpointSummary("Returns the scraper error history")]
        [ProducesResponseType(typeof(AdminScraperErrorsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminScraperErrorsResponse>> GetScraperErrors(
            [FromQuery]int? scrapeLogId = null,
            [FromQuery]int page = 1,
            [FromQuery]int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            ScraperErrorsPage result = await scraperQueryService.GetErrorsAsync(
                scrapeLogId,
                page,
                pageSize,
                cancellationToken);

            return Ok(new AdminScraperErrorsResponse(
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.Items
                      .Select(entry => new AdminScraperErrorItem(
                          entry.ErrorId,
                          entry.ScrapeLogId,
                          entry.Scope,
                          entry.ErrorType,
                          entry.Message,
                          entry.PageTitle,
                          entry.ItemName,
                          entry.OccurredAt))
                      .ToList()));
        }

        [HttpPost("scraper/run")]
        [EndpointSummary("Starts the scraper manually")]
        [ProducesResponseType(typeof(AdminScraperRunResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(AdminScraperRunResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AdminScraperRunResponse>> RunScraper(
            [FromBody]AdminRunScraperRequest request,
            CancellationToken cancellationToken = default)
        {
            ScraperStartResult result = await scraperRuntimeService.StartAsync(
                new ScraperRunRequest(
                    request.Force,
                    request.ScraperName,
                    request.CategorySlug,
                    request.TriggeredBy ?? "Admin"),
                cancellationToken);

            AdminScraperRunResponse response = new(
                ResolveStatus(result.Status),
                result.Message,
                result.Status.ActiveScrapeLogId);

            return result.Started ? Accepted(response) : Conflict(response);
        }

        [HttpPost("scraper/stop")]
        [EndpointSummary("Stops an active scraper run")]
        [ProducesResponseType(typeof(AdminScraperStopResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(AdminScraperStopResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AdminScraperStopResponse>> StopScraper(
            [FromBody]AdminStopScraperRequest request,
            CancellationToken cancellationToken = default)
        {
            ScraperRuntimeStatus currentStatus = scraperRuntimeService.GetStatus();

            if(request.ScrapeLogId.HasValue &&
               !currentStatus.ActiveScrapeLogIds.Contains(request.ScrapeLogId.Value))
            {
                return Conflict(new AdminScraperStopResponse(
                    ResolveStatus(currentStatus),
                    $"The requested scraper log id {request.ScrapeLogId} is not part of the active scraper run.",
                    currentStatus.ActiveScrapeLogId));
            }

            if(!string.IsNullOrWhiteSpace(request.ScraperName) &&
               !currentStatus.ActiveScrapers.Any(entry =>
               string.Equals(entry.ScraperName, request.ScraperName, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new AdminScraperStopResponse(
                    ResolveStatus(currentStatus),
                    $"The requested scraper {request.ScraperName} is not part of the active scraper run.",
                    currentStatus.ActiveScrapeLogId));
            }

            if(!string.IsNullOrWhiteSpace(request.CategorySlug) &&
               !currentStatus.ActiveScrapers.Any(entry =>
               string.Equals(entry.CategorySlug, request.CategorySlug, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new AdminScraperStopResponse(
                    ResolveStatus(currentStatus),
                    $"The requested category slug {request.CategorySlug} is not part of the active scraper run.",
                    currentStatus.ActiveScrapeLogId));
            }

            ScraperStopResult result = await scraperRuntimeService.StopAsync(
                new ScraperStopRequest(
                    request.Reason,
                    request.RequestedBy ?? "Admin"),
                cancellationToken);

            AdminScraperStopResponse response = new(
                ResolveStatus(result.Status),
                result.Message,
                result.Status.ActiveScrapeLogId);

            return result.StopRequested ? Accepted(response) : Conflict(response);
        }

        [HttpGet("background-jobs/scheduled-scraper")]
        [EndpointSummary("Returns the persisted daily scheduled scraper settings")]
        [ProducesResponseType(typeof(AdminScheduledScraperSettingsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminScheduledScraperSettingsResponse>> GetScheduledScraperSettings(
            CancellationToken cancellationToken = default)
        {
            ScheduledScraperSchedule schedule = await scheduledScraperConfigurationService.GetAsync(cancellationToken);
            return Ok(MapScheduledScraperSettings(schedule));
        }

        [HttpPut("background-jobs/scheduled-scraper")]
        [EndpointSummary("Updates the daily scheduled scraper settings")]
        [ProducesResponseType(typeof(AdminScheduledScraperSettingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AdminScheduledScraperSettingsResponse>> UpdateScheduledScraperSettings(
            [FromBody]AdminUpdateScheduledScraperSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            if(request.ScheduleHour is < 0 or > 23)
            {
                return BadRequest("ScheduleHour must be between 0 and 23.");
            }

            if(request.ScheduleMinute is < 0 or > 59)
            {
                return BadRequest("ScheduleMinute must be between 0 and 59.");
            }

            ScheduledScraperSchedule schedule = await scheduledScraperConfigurationService.UpdateAsync(
                request.Enabled,
                request.ScheduleHour,
                request.ScheduleMinute,
                cancellationToken);

            return Ok(MapScheduledScraperSettings(schedule));
        }

        [HttpGet("stats/api")]
        [EndpointSummary("Returns API usage statistics for the admin area")]
        [ProducesResponseType(typeof(AdminApiStatsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminApiStatsResponse>> GetApiStats(
            [FromQuery]int days = 30,
            CancellationToken cancellationToken = default)
        {
            ApiStatisticsSummary result = await apiStatisticsService.GetSummaryAsync(days, cancellationToken);

            return Ok(new AdminApiStatsResponse(
                result.Days,
                result.TotalRequests,
                result.UniqueIpCount,
                result.ErrorCount,
                result.BlockedCount,
                result.AverageResponseTimeMs,
                result.TotalResponseSizeBytes,
                result.AverageResponseSizeBytes,
                result.CacheHitCount,
                result.CacheMissCount,
                result.CacheBypassCount,
                result.PeakRequestHourUtc,
                result.Daily
                      .Select(entry => new AdminApiStatBucket(
                          entry.Day,
                          entry.RequestCount,
                          entry.ErrorCount,
                          entry.BlockedCount,
                          entry.CacheHitCount,
                          entry.CacheMissCount,
                          entry.CacheBypassCount,
                          entry.TotalResponseSizeBytes,
                          entry.AverageResponseTimeMs))
                      .ToList(),
                result.TopEndpoints
                      .Select(entry => new AdminEndpointStatItem(
                          entry.Route,
                          entry.Method,
                          entry.RequestCount,
                          entry.AverageResponseTimeMs))
                      .ToList(),
                result.TopStatusCodes
                      .Select(entry => new AdminStatusCodeStatItem(
                          entry.StatusCode,
                          entry.RequestCount))
                      .ToList(),
                result.TopIps
                      .Select(entry => new AdminIpStatItem(
                          entry.IpAddress,
                          entry.RequestCount,
                          entry.BlockedCount))
                      .ToList()));
        }

        [HttpGet("system/redis")]
        [EndpointSummary("Returns the Redis readiness status used by the API")]
        [ProducesResponseType(typeof(AdminRedisStatusResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminRedisStatusResponse>> GetRedisStatus(CancellationToken cancellationToken = default)
        {
            HealthReport report = await healthCheckService.CheckHealthAsync(
                registration => string.Equals(registration.Name, "redis", StringComparison.OrdinalIgnoreCase),
                cancellationToken);

            HealthReportEntry? redisEntry = report.Entries.TryGetValue("redis", out HealthReportEntry entry)
            ? entry
            : null;

            string message = redisEntry?.Description
                             ?? report.Status switch
                             {
                                 HealthStatus.Healthy => "Redis cache is healthy.",
                                 HealthStatus.Degraded => "Redis cache reported a degraded state.",
                                 _ => "Redis cache is unavailable."
                             };

            return Ok(new AdminRedisStatusResponse(
                report.Status.ToString(),
                cachingOptions.UseRedisForHybridCache,
                cachingOptions.UseRedisForOutputCache,
                cachingOptions.RedisInstanceName,
                message,
                DateTime.UtcNow));
        }

        [HttpGet("system/metrics")]
        [EndpointSummary("Returns a parsed Prometheus metrics overview for the admin dashboard")]
        [ProducesResponseType(typeof(AdminMetricsOverviewResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminMetricsOverviewResponse>> GetMetricsOverview(CancellationToken cancellationToken = default)
        {
            return Ok(await adminMetricsService.GetOverviewAsync(cancellationToken));
        }

        [HttpGet("system/database-load")]
        [EndpointSummary("Returns a live rolling summary of observed database command load")]
        [ProducesResponseType(typeof(AdminDatabaseLoadResponse), StatusCodes.Status200OK)]
        public ActionResult<AdminDatabaseLoadResponse> GetDatabaseLoad()
        {
            DatabaseLoadSnapshot snapshot = databaseLoadMonitor.GetSnapshot();

            return Ok(new AdminDatabaseLoadResponse(
                snapshot.CollectedAtUtc,
                snapshot.WindowMinutes,
                snapshot.TotalCommands,
                snapshot.AverageDurationMs,
                snapshot.MaxDurationMs,
                snapshot.SlowCommandCount,
                snapshot.FailedCommandCount,
                snapshot.CommandsPerMinute,
                snapshot.TopCommands
                        .Select(entry => new AdminDatabaseCommandStatItem(
                            entry.CommandText,
                            entry.Count,
                            entry.AverageDurationMs,
                            entry.MaxDurationMs,
                            entry.FailedCount,
                            entry.LastSeenAtUtc))
                        .ToList(),
                snapshot.RecentSlowCommands
                        .Select(entry => new AdminDatabaseCommandSampleItem(
                            entry.OccurredAtUtc,
                            entry.CommandText,
                            entry.DurationMs,
                            entry.Failed))
                        .ToList()));
        }

        [HttpGet("security/protection-rules")]
        [EndpointSummary("Returns the active rate-limit, ban and block escalation rules")]
        [ProducesResponseType(typeof(AdminProtectionRulesResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminProtectionRulesResponse>> GetProtectionRules(CancellationToken cancellationToken = default)
        {
            RequestProtectionSettingsSnapshot settings = await requestProtectionConfigurationService.GetSnapshotAsync(cancellationToken);

            return Ok(new AdminProtectionRulesResponse(
                RequestProtectionPolicyDescriptors.Rules
                                                  .Select(rule => new AdminProtectionRuleItem(
                                                      rule.Outcome,
                                                      rule.StatusCode,
                                                      rule.Trigger,
                                                      rule.Effect))
                                                  .ToList(),
                [
                    MapPolicy("Public API", settings.PublicApi),
                    MapPolicy("Admin Read API", settings.AdminReadApi),
                    MapPolicy("Admin Mutation API", settings.AdminMutationApi),
                    MapPolicy("Admin Login", settings.AdminLogin),
                    MapPolicy("Health API", settings.HealthApi)
                ]));
        }

        [HttpGet("security/rate-limit-settings")]
        [EndpointSummary("Returns the editable request protection settings")]
        [ProducesResponseType(typeof(AdminRateLimitSettingsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminRateLimitSettingsResponse>> GetRateLimitSettings(
            CancellationToken cancellationToken = default)
        {
            RequestProtectionSettingsSnapshot settings = await requestProtectionConfigurationService.GetSnapshotAsync(cancellationToken);
            return Ok(MapRateLimitSettings(settings));
        }

        [HttpPut("security/rate-limit-settings")]
        [EndpointSummary("Updates the editable request protection settings")]
        [ProducesResponseType(typeof(AdminRateLimitSettingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AdminRateLimitSettingsResponse>> UpdateRateLimitSettings(
            [FromBody]AdminUpdateRateLimitSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                RequestProtectionSettingsSnapshot settings = await requestProtectionConfigurationService.UpdateAsync(
                    request.Enabled,
                    request.Policies
                           .Select(policy => new RequestProtectionPolicyUpdate(
                               policy.ScopeKey,
                               policy.TokenLimit,
                               policy.TokensPerPeriod,
                               policy.ReplenishmentSeconds,
                               policy.TokenQueueLimit,
                               policy.ConcurrentPermitLimit,
                               policy.ConcurrentQueueLimit))
                           .ToList(),
                    cancellationToken);

                return Ok(MapRateLimitSettings(settings));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("stats/requests")]
        [EndpointSummary("Returns recent API request log entries")]
        [ProducesResponseType(typeof(AdminApiRequestLogResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminApiRequestLogResponse>> GetRequestLogs(
            [FromQuery]int days = 1,
            [FromQuery]string? ipAddress = null,
            [FromQuery]int page = 1,
            [FromQuery]int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            ApiRequestLogPage result = await apiStatisticsService.GetRequestLogsAsync(
                days,
                ipAddress,
                page,
                pageSize,
                cancellationToken);

            return Ok(new AdminApiRequestLogResponse(
                result.Days,
                result.FilteredIpAddress,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.Items
                      .Select(entry => new AdminApiRequestLogItem(
                          entry.RequestId,
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
                      .ToList()));
        }

        [HttpGet("stats/ip-details")]
        [EndpointSummary("Returns detailed activity data for a single IP address")]
        [ProducesResponseType(typeof(AdminIpActivityResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AdminIpActivityResponse>> GetIpActivity(
            [FromQuery]string ipAddress,
            [FromQuery]int days = 1,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(ipAddress))
            {
                return BadRequest("An ipAddress query value is required.");
            }

            ApiIpActivityDetails result = await apiStatisticsService.GetIpActivityAsync(
                ipAddress,
                days,
                cancellationToken: cancellationToken);

            return Ok(new AdminIpActivityResponse(
                result.IpAddress,
                result.Days,
                result.TotalRequests,
                result.BlockedCount,
                result.ErrorCount,
                result.AverageResponseTimeMs,
                result.TotalResponseSizeBytes,
                result.AverageResponseSizeBytes,
                result.FirstSeenAt,
                result.LastSeenAt,
                result.TopEndpoints
                      .Select(entry => new AdminEndpointStatItem(
                          entry.Route,
                          entry.Method,
                          entry.RequestCount,
                          entry.AverageResponseTimeMs))
                      .ToList(),
                result.RecentRequests
                      .Select(entry => new AdminApiRequestLogItem(
                          entry.RequestId,
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
                      .ToList()));
        }

        [HttpGet("bans")]
        [EndpointSummary("Returns the currently configured IP bans")]
        [ProducesResponseType(typeof(AdminIpBanListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminIpBanListResponse>> GetIpBans(
            [FromQuery]bool includeExpired = false,
            [FromQuery]int page = 1,
            [FromQuery]int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            IpBanPage result = await ipBanService.GetBansAsync(includeExpired, page, pageSize, cancellationToken);

            return Ok(new AdminIpBanListResponse(
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.Items
                      .Select(entry => new AdminIpBanItem(
                          entry.IpAddress,
                          entry.Reason,
                          entry.IsActive,
                          entry.StartedAt,
                          entry.EndsAt,
                          entry.DurationMinutes,
                          entry.CreatedBy))
                      .ToList()));
        }

        [HttpPost("bans")]
        [EndpointSummary("Bans an IP address")]
        [ProducesResponseType(typeof(AdminIpBanMutationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AdminIpBanMutationResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AdminIpBanMutationResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AdminIpBanMutationResponse>> BanIp(
            [FromBody]AdminBanIpRequest request,
            CancellationToken cancellationToken = default)
        {
            string requesterIp = RequestProtectionClassifier.ResolveClientIp(HttpContext);

            if(RequestProtectionClassifier.IsLoopbackAddress(request.IpAddress))
            {
                return BadRequest(new AdminIpBanMutationResponse(
                    "ProtectedIpAddress",
                    "Loopback and localhost addresses cannot be banned.",
                    request.IpAddress));
            }

            if(RequestProtectionClassifier.AreEquivalentIpAddresses(request.IpAddress, requesterIp))
            {
                return Conflict(new AdminIpBanMutationResponse(
                    "OwnIpAddress",
                    "The current admin session IP address cannot ban itself.",
                    request.IpAddress));
            }

            IpBanMutationResult result = await ipBanService.BanAsync(
                new IpBanMutationRequest(
                    request.IpAddress,
                    request.Reason,
                    request.ExpiresAt,
                    request.DurationMinutes,
                    request.CreatedBy),
                cancellationToken);

            AdminIpBanMutationResponse response = new(
                ResolveBanMutationStatus(result.Outcome, "Banned"),
                result.Message,
                result.IpAddress ?? request.IpAddress);

            return result.Outcome switch
            {
                IpBanMutationOutcome.Success => Ok(response),
                IpBanMutationOutcome.InvalidIp => BadRequest(response),
                IpBanMutationOutcome.InvalidBanWindow => BadRequest(response),
                IpBanMutationOutcome.ProtectedIp => BadRequest(response),
                IpBanMutationOutcome.AlreadyExists => Conflict(response),
                _ => Conflict(response)
            };
        }

        [HttpDelete("bans")]
        [EndpointSummary("Unbans an IP address")]
        [ProducesResponseType(typeof(AdminIpBanMutationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AdminIpBanMutationResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AdminIpBanMutationResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdminIpBanMutationResponse>> UnbanIp(
            [FromBody]AdminUnbanIpRequest request,
            CancellationToken cancellationToken = default)
        {
            IpBanMutationResult result = await ipBanService.UnbanAsync(
                new IpBanRemovalRequest(
                    request.IpAddress,
                    request.Reason,
                    request.RequestedBy),
                cancellationToken);

            AdminIpBanMutationResponse response = new(
                ResolveBanMutationStatus(result.Outcome, "Unbanned"),
                result.Message,
                result.IpAddress ?? request.IpAddress);

            return result.Outcome switch
            {
                IpBanMutationOutcome.Success => Ok(response),
                IpBanMutationOutcome.InvalidIp => BadRequest(response),
                IpBanMutationOutcome.NotFound => NotFound(response),
                _ => Conflict(response)
            };
        }

        private static AdminScraperStatusResponse MapStatusResponse(ScraperRuntimeStatus status)
        {
            return new AdminScraperStatusResponse(
                ResolveStatus(status),
                status.IsRunning,
                status.StopRequested,
                status.ActiveScrapeLogId,
                status.ActiveScrapeLogIds,
                status.TriggeredBy,
                status.StartedAt,
                status.FinishedAt,
                status.LastResult,
                status.CurrentScraperName,
                status.CurrentCategoryName,
                status.CurrentCategorySlug,
                status.TotalScrapers,
                status.CompletedScrapers,
                status.ActiveScraperCount,
                status.ActiveScrapers
                      .Select(entry => new AdminActiveScraperItem(
                          entry.ScrapeLogId,
                          entry.ScraperName,
                          entry.CategoryName,
                          entry.CategorySlug,
                          entry.StartedAt))
                      .ToList(),
                status.LastMessage,
                status.StopReason);
        }

        private static string ResolveStatus(ScraperRuntimeStatus status)
        {
            if(status.IsRunning)
            {
                return status.StopRequested ? "CancellationRequested" : "Running";
            }

            return status.LastResult ?? "Pending";
        }

        private static string ResolveBanMutationStatus(IpBanMutationOutcome outcome, string successStatus)
        {
            return outcome switch
            {
                IpBanMutationOutcome.Success => successStatus,
                IpBanMutationOutcome.InvalidIp => "InvalidIpAddress",
                IpBanMutationOutcome.InvalidBanWindow => "InvalidBanWindow",
                IpBanMutationOutcome.ProtectedIp => "ProtectedIpAddress",
                IpBanMutationOutcome.AlreadyExists => "AlreadyBanned",
                IpBanMutationOutcome.NotFound => "NotFound",
                _ => "Unknown"
            };
        }

        private static AdminScheduledScraperSettingsResponse MapScheduledScraperSettings(ScheduledScraperSchedule schedule)
        {
            return new AdminScheduledScraperSettingsResponse(
                schedule.Enabled,
                schedule.ScheduleHour,
                schedule.ScheduleMinute,
                schedule.TimeoutMinutes,
                schedule.LastTriggeredAtUtc);
        }

        private static AdminRateLimitSettingsResponse MapRateLimitSettings(RequestProtectionSettingsSnapshot settings)
        {
            return new AdminRateLimitSettingsResponse(
                settings.Enabled,
                settings.Version,
                settings.UpdatedAtUtc,
                [
                    MapConfigurablePolicy(RequestProtectionPolicyScopes.PublicApiScopeKey, "Public API", settings.PublicApi),
                    MapConfigurablePolicy(RequestProtectionPolicyScopes.AdminReadApiScopeKey, "Admin Read API", settings.AdminReadApi),
                    MapConfigurablePolicy(RequestProtectionPolicyScopes.AdminMutationApiScopeKey, "Admin Mutation API", settings.AdminMutationApi),
                    MapConfigurablePolicy(RequestProtectionPolicyScopes.AdminLoginScopeKey, "Admin Login", settings.AdminLogin),
                    MapConfigurablePolicy(RequestProtectionPolicyScopes.HealthApiScopeKey, "Health API", settings.HealthApi)
                ]);
        }

        private static AdminRateLimitPolicyItem MapPolicy(string scope, RequestProtectionProfile profile)
        {
            return new AdminRateLimitPolicyItem(
                scope,
                profile.TokenLimit,
                profile.TokensPerPeriod,
                profile.ReplenishmentSeconds,
                profile.TokenQueueLimit,
                profile.ConcurrentPermitLimit,
                profile.ConcurrentQueueLimit);
        }

        private static AdminConfigurableRateLimitPolicyItem MapConfigurablePolicy(
            string scopeKey,
            string scope,
            RequestProtectionProfile profile)
        {
            return new AdminConfigurableRateLimitPolicyItem(
                scopeKey,
                scope,
                profile.TokenLimit,
                profile.TokensPerPeriod,
                profile.ReplenishmentSeconds,
                profile.TokenQueueLimit,
                profile.ConcurrentPermitLimit,
                profile.ConcurrentQueueLimit);
        }
    }
}
