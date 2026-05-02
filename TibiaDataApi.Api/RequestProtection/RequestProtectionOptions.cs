namespace TibiaDataApi.RequestProtection
{
    public sealed class RequestProtectionOptions
    {
        public const string SectionName = "RequestProtection";

        public bool Enabled { get; set; } = true;

        public RequestProtectionProfile PublicApi { get; set; } = new();

        public RequestProtectionProfile AdminReadApi { get; set; } = new()
        {
            TokenLimit = 60,
            TokensPerPeriod = 30,
            ReplenishmentSeconds = 60,
            ConcurrentPermitLimit = 4
        };

        public RequestProtectionProfile AdminMutationApi { get; set; } = new()
        {
            TokenLimit = 12,
            TokensPerPeriod = 12,
            ReplenishmentSeconds = 60,
            ConcurrentPermitLimit = 1
        };

        public RequestProtectionProfile AdminLogin { get; set; } = new()
        {
            TokenLimit = 5,
            TokensPerPeriod = 5,
            ReplenishmentSeconds = 300,
            ConcurrentPermitLimit = 1
        };

        public RequestProtectionProfile HealthApi { get; set; } = new()
        {
            TokenLimit = 24,
            TokensPerPeriod = 12,
            ReplenishmentSeconds = 60,
            ConcurrentPermitLimit = 2
        };
    }

    public sealed class RequestProtectionProfile
    {
        public int TokenLimit { get; set; } = 120;

        public int TokensPerPeriod { get; set; } = 60;

        public int ReplenishmentSeconds { get; set; } = 60;

        public int TokenQueueLimit { get; set; } = 0;

        public int ConcurrentPermitLimit { get; set; } = 8;

        public int ConcurrentQueueLimit { get; set; } = 0;
    }
}