namespace TibiaDataApi.Services.Caching
{
    public sealed class CachingOptions
    {
        public const string SectionName = "Caching";

        public string RedisConnectionStringName { get; set; } = "Redis";

        public string RedisInstanceName { get; set; } = "TibiaDataApi";

        public bool UseRedisForHybridCache { get; set; } = true;

        public bool UseRedisForOutputCache { get; set; } = true;

        public HybridCacheSettings HybridCache { get; set; } = new();

        public CacheProfileSettings IpBan { get; set; } = new()
        {
            ExpirationSeconds = 120,
            LocalExpirationSeconds = 30
        };

        public CacheProfileSettings ScraperQuery { get; set; } = new()
        {
            ExpirationSeconds = 15,
            LocalExpirationSeconds = 5
        };

        public CacheProfileSettings ApiStatistics { get; set; } = new()
        {
            ExpirationSeconds = 30,
            LocalExpirationSeconds = 15
        };

        public OutputCacheSettings OutputCache { get; set; } = new();
    }

    public sealed class HybridCacheSettings
    {
        public int DefaultExpirationSeconds { get; set; } = 300;

        public int DefaultLocalExpirationSeconds { get; set; } = 60;

        public long MaximumPayloadBytes { get; set; } = 1024 * 1024;

        public int MaximumKeyLength { get; set; } = 1024;
    }

    public sealed class CacheProfileSettings
    {
        public int ExpirationSeconds { get; set; } = 60;

        public int LocalExpirationSeconds { get; set; } = 15;
    }

    public sealed class OutputCacheSettings
    {
        public int DefaultExpirationSeconds { get; set; } = 60;

        public int PublicOpenApiSeconds { get; set; } = 300;

        public int PublicScalarSeconds { get; set; } = 120;

        public int PublicApiSeconds { get; set; } = 120;

        public int ReferenceDataSeconds { get; set; } = 600;
    }
}