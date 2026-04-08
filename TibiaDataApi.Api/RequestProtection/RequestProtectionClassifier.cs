using System.Net;

using TibiaDataApi.AdminAccess;

namespace TibiaDataApi.RequestProtection
{
    internal static class RequestProtectionClassifier
    {
        public static bool IsTrackedApiRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
                   !ShouldBypassProtection(context) &&
                   !IsAdminApiRequest(context);
        }

        public static string ResolveClientIp(HttpContext context)
        {
            IPAddress? resolvedAddress = ResolveClientIpAddress(context);
            return resolvedAddress?.ToString() ?? "unknown";
        }

        public static bool ShouldBypassProtection(HttpContext context)
        {
            return IsLocalRequest(context) || IsAdminAuthenticated(context) || IsAdminSurfaceRequest(context) || IsAdminApiRequest(context);
        }

        public static bool IsLocalRequest(HttpContext context)
        {
            return IsLoopbackAddress(ResolveClientIp(context));
        }

        public static bool IsLoopbackAddress(string? ipAddress)
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

            if(IPAddress.IsLoopback(parsed))
            {
                return true;
            }

            return parsed.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(parsed.MapToIPv4());
        }

        public static bool AreEquivalentIpAddresses(string? left, string? right)
        {
            if(string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            if(string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if(!IPAddress.TryParse(left.Trim(), out IPAddress? leftAddress) ||
               !IPAddress.TryParse(right.Trim(), out IPAddress? rightAddress))
            {
                return false;
            }

            if(leftAddress.Equals(rightAddress))
            {
                return true;
            }

            if(leftAddress.IsIPv4MappedToIPv6)
            {
                leftAddress = leftAddress.MapToIPv4();
            }

            if(rightAddress.IsIPv4MappedToIPv6)
            {
                rightAddress = rightAddress.MapToIPv4();
            }

            return leftAddress.Equals(rightAddress);
        }

        public static RequestProtectionScope Classify(HttpContext context)
        {
            if(ShouldBypassProtection(context))
            {
                return RequestProtectionScope.None;
            }

            PathString path = context.Request.Path;

            if(path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            {
                return RequestProtectionScope.HealthApi;
            }

            return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            ? RequestProtectionScope.PublicApi
            : RequestProtectionScope.None;
        }

        public static bool IsBlocked(HttpContext context)
        {
            return context.Items.ContainsKey(RequestProtectionContext.BlockReasonItemKey);
        }

        public static void MarkBlocked(HttpContext context, string reason)
        {
            context.Items[RequestProtectionContext.BlockReasonItemKey] = reason;
        }

        private static bool IsAdminAuthenticated(HttpContext context)
        {
            return context.User.Identity?.IsAuthenticated == true &&
                   context.User.HasClaim(AdminAccessDefaults.ClaimType, AdminAccessDefaults.ClaimValue);
        }

        private static bool IsAdminSurfaceRequest(HttpContext context)
        {
            PathString path = context.Request.Path;

            return path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWithSegments(AdminAccessDefaults.AdminScalarPath, StringComparison.OrdinalIgnoreCase) ||
                   path.Equals(AdminAccessDefaults.AdminOpenApiPath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAdminApiRequest(HttpContext context)
        {
            PathString path = context.Request.Path;

            return path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase) ||
                   path.Equals("/api/scraper/quick-test", StringComparison.OrdinalIgnoreCase);
        }

        private static IPAddress? ResolveClientIpAddress(HttpContext context)
        {
            return context.Connection.RemoteIpAddress;
        }
    }
}