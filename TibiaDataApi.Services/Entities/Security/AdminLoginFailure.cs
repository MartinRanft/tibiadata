namespace TibiaDataApi.Services.Entities.Security
{
    public sealed class AdminLoginFailure
    {
        public string IpAddress { get; set; } = string.Empty;

        public int FailedAttempts { get; set; }

        public DateTime FirstFailedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastFailedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}