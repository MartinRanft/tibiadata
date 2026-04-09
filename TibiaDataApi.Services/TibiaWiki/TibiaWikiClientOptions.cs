namespace TibiaDataApi.Services.TibiaWiki
{
    public sealed class TibiaWikiClientOptions
    {
        public const string SectionName = "TibiaWiki";

        public int RequestTimeoutSeconds { get; set; } = 30;

        public int MaxRetryAttempts { get; set; } = 3;

        public int BaseDelayMilliseconds { get; set; } = 500;

        public int MaxDelayMilliseconds { get; set; } = 5000;

        public int MaxJitterMilliseconds { get; set; } = 250;
    }
}