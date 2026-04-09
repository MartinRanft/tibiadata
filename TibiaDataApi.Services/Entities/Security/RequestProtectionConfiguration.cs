namespace TibiaDataApi.Services.Entities.Security
{
    public sealed class RequestProtectionConfiguration
    {
        public string Key { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        public int Version { get; set; } = 1;

        public int PublicApiTokenLimit { get; set; } = 120;

        public int PublicApiTokensPerPeriod { get; set; } = 60;

        public int PublicApiReplenishmentSeconds { get; set; } = 60;

        public int PublicApiTokenQueueLimit { get; set; } = 0;

        public int PublicApiConcurrentPermitLimit { get; set; } = 8;

        public int PublicApiConcurrentQueueLimit { get; set; } = 0;

        public int AdminReadApiTokenLimit { get; set; } = 60;

        public int AdminReadApiTokensPerPeriod { get; set; } = 30;

        public int AdminReadApiReplenishmentSeconds { get; set; } = 60;

        public int AdminReadApiTokenQueueLimit { get; set; } = 0;

        public int AdminReadApiConcurrentPermitLimit { get; set; } = 4;

        public int AdminReadApiConcurrentQueueLimit { get; set; } = 0;

        public int AdminMutationApiTokenLimit { get; set; } = 12;

        public int AdminMutationApiTokensPerPeriod { get; set; } = 12;

        public int AdminMutationApiReplenishmentSeconds { get; set; } = 60;

        public int AdminMutationApiTokenQueueLimit { get; set; } = 0;

        public int AdminMutationApiConcurrentPermitLimit { get; set; } = 1;

        public int AdminMutationApiConcurrentQueueLimit { get; set; } = 0;

        public int AdminLoginTokenLimit { get; set; } = 5;

        public int AdminLoginTokensPerPeriod { get; set; } = 5;

        public int AdminLoginReplenishmentSeconds { get; set; } = 300;

        public int AdminLoginTokenQueueLimit { get; set; } = 0;

        public int AdminLoginConcurrentPermitLimit { get; set; } = 1;

        public int AdminLoginConcurrentQueueLimit { get; set; } = 0;

        public int HealthApiTokenLimit { get; set; } = 24;

        public int HealthApiTokensPerPeriod { get; set; } = 12;

        public int HealthApiReplenishmentSeconds { get; set; } = 60;

        public int HealthApiTokenQueueLimit { get; set; } = 0;

        public int HealthApiConcurrentPermitLimit { get; set; } = 2;

        public int HealthApiConcurrentQueueLimit { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
