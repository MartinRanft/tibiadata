using System.Text.Json;

using TibiaDataApi.RequestProtection;
using TibiaDataApi.Services.Admin.Security;
using TibiaDataApi.Services.Admin.Statistics;
using TibiaDataApi.Services.Entities.Monitoring;

namespace TibiaDataApi.Middleware
{
    public sealed class IpBanMiddleware(
        RequestDelegate next,
        ILogger<IpBanMiddleware> logger)
    {
        private readonly ILogger<IpBanMiddleware> _logger = logger;
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(
            HttpContext context,
            IIpBanService ipBanService,
            IApiStatisticsService apiStatisticsService)
        {
            if(!RequestProtectionClassifier.IsTrackedApiRequest(context))
            {
                await _next(context);
                return;
            }

            string ipAddress = RequestProtectionClassifier.ResolveClientIp(context);

            bool isBlocked;
            try
            {
                isBlocked = await ipBanService.IsBlockedAsync(ipAddress, context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IP ban lookup failed. Request will continue.");
                await _next(context);
                return;
            }

            if(!isBlocked)
            {
                await _next(context);
                return;
            }

            RequestProtectionClassifier.MarkBlocked(context, RequestProtectionContext.IpBanBlockReason);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            try
            {
                await apiStatisticsService.RecordRequestAsync(
                    new ApiRequestRecord(
                        ipAddress,
                        context.Request.Method,
                        context.Request.Path.Value ?? "/unknown",
                        StatusCodes.Status403Forbidden,
                        0,
                        context.Request.Headers.UserAgent.ToString(),
                        0,
                        ApiCacheStatus.Bypass,
                        true,
                        DateTime.UtcNow),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist blocked request statistics.");
            }

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    error = "forbidden",
                    message = "Access denied."
                }),
                CancellationToken.None);
        }
    }
}
