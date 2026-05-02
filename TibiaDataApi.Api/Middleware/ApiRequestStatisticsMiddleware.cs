using System.Diagnostics;

using TibiaDataApi.RequestProtection;
using TibiaDataApi.Services.Admin.Statistics;
using TibiaDataApi.Services.Entities.Monitoring;

namespace TibiaDataApi.Middleware
{
    public sealed class ApiRequestStatisticsMiddleware(
        RequestDelegate next,
        ILogger<ApiRequestStatisticsMiddleware> logger)
    {
        private readonly ILogger<ApiRequestStatisticsMiddleware> _logger = logger;
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context, IApiStatisticsService apiStatisticsService)
        {
            if(!RequestProtectionClassifier.IsTrackedApiRequest(context))
            {
                await _next(context);
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            int statusCode = StatusCodes.Status200OK;

            try
            {
                await _next(context);
                statusCode = context.Response.StatusCode;
            }
            catch (Exception ex)
            {
                statusCode = StatusCodes.Status500InternalServerError;
                _logger.LogWarning(ex, "API request failed while collecting request statistics.");
                throw;
            }
            finally
            {
                stopwatch.Stop();

                try
                {
                    await apiStatisticsService.RecordRequestAsync(
                        new ApiRequestRecord(
                            RequestProtectionClassifier.ResolveClientIp(context),
                            context.Request.Method,
                            ResolveRoute(context),
                            statusCode,
                            stopwatch.Elapsed.TotalMilliseconds,
                            ResolveUserAgent(context),
                            ResolveResponseSizeBytes(context),
                            ResolveCacheStatus(context),
                            RequestProtectionClassifier.IsBlocked(context) || statusCode == StatusCodes.Status429TooManyRequests,
                            DateTime.UtcNow),
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist API request statistics.");
                }
            }
        }

        private static string ResolveRoute(HttpContext context)
        {
            if(context.GetEndpoint() is RouteEndpoint routeEndpoint &&
               !string.IsNullOrWhiteSpace(routeEndpoint.RoutePattern.RawText))
            {
                return "/" + routeEndpoint.RoutePattern.RawText.TrimStart('/');
            }

            return string.IsNullOrWhiteSpace(context.Request.Path.Value)
            ? "/unknown"
            : context.Request.Path.Value!;
        }

        private static string? ResolveUserAgent(HttpContext context)
        {
            string? userAgent = context.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
        }

        private static long ResolveResponseSizeBytes(HttpContext context)
        {
            if(context.Response.ContentLength is long contentLength && contentLength >= 0)
            {
                return contentLength;
            }

            return long.TryParse(context.Response.Headers.ContentLength.ToString(), out long parsedValue) && parsedValue >= 0
            ? parsedValue
            : 0;
        }

        private static string ResolveCacheStatus(HttpContext context)
        {
            if(context.Response.Headers.TryGetValue("Age", out _))
            {
                return ApiCacheStatus.Hit;
            }

            return HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)
            ? ApiCacheStatus.Miss
            : ApiCacheStatus.Bypass;
        }
    }
}
