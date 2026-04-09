namespace TibiaDataApi.Services.Entities.Security
{
    public class IpBan
    {
        public int Id { get; set; }

        public string IpAddress { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        public int? DurationMinutes { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? RevokedBy { get; set; }

        public string? RevocationReason { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
