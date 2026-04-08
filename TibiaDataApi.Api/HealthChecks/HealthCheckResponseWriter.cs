using System.Text.Json;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TibiaDataApi.HealthChecks
{
    internal static class HealthCheckResponseWriter
    {
        public static async Task WriteAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            object payload = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration.TotalMilliseconds,
                    tags = entry.Value.Tags,
                    data = entry.Value.Data.ToDictionary(item => item.Key, item => item.Value)
                })
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}