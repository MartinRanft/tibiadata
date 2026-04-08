namespace TibiaDataApi.RequestProtection
{
    internal static class RequestProtectionContext
    {
        public const string BlockReasonItemKey = "TibiaDataApi.RequestProtection.BlockReason";
        public const string IpBanBlockReason = "IpBan";
        public const string RateLimitBlockReason = "RateLimit";
    }
}