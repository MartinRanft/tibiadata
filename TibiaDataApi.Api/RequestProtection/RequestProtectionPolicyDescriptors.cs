using TibiaDataApi.Services.Admin.Security;

namespace TibiaDataApi.RequestProtection
{
    internal static class RequestProtectionPolicyDescriptors
    {
        public static readonly IReadOnlyList<RequestProtectionRuleDescriptor> Rules =
        [
            new(
                "429 Too Many Requests",
                StatusCodes.Status429TooManyRequests,
                "A request exceeds the configured token-bucket or concurrency limits for its protection scope.",
                "Request rejected immediately. A Retry-After header is emitted when a retry window is known."),
            new(
                "403 Forbidden",
                StatusCodes.Status403Forbidden,
                "The client IP is covered by an active IP ban. This includes manual bans and automatic admin-login lockouts.",
                "All protected API access from that IP is denied until the ban expires or an admin removes it."),
            new(
                "Complete IP Block",
                StatusCodes.Status403Forbidden,
                $"Manual bans apply instantly. Automatic admin-login lockout is triggered after {AdminLoginProtectionService.FailureThreshold} failed password attempts within {AdminLoginProtectionService.FailureWindow.TotalMinutes:0} minutes.",
                $"Automatic login lockouts currently ban the IP for {AdminLoginProtectionService.FailureWindow.TotalMinutes:0} minutes.")
        ];
    }

    internal sealed record RequestProtectionRuleDescriptor(
        string Outcome,
        int StatusCode,
        string Trigger,
        string Effect);
}
