using TibiaDataApi.Services.Entities.Creatures;

namespace TibiaDataApi.Services.Entities.Assets
{
    public class CreatureImageSyncQueueEntry
    {
        public int CreatureId { get; set; }

        public Creature? Creature { get; set; }

        public required string WikiPageTitle { get; set; }

        public ItemImageSyncState Status { get; set; } = ItemImageSyncState.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastAttemptedAt { get; set; }

        public DateTime? LastCompletedAt { get; set; }

        public int RetryCount { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}