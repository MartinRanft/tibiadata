using TibiaDataApi.Services.Entities.Assets;

namespace TibiaDataApi.Services.Entities.Creatures
{
    public class Creature
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string NormalizedName { get; set; }

        public int Hitpoints { get; set; }

        public long Experience { get; set; }

        
        public string? BestiaryJson { get; set; }

        public string? LootStatisticsJson { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public List<CreatureAsset> CreatureAssets { get; set; } = [];

        public CreatureImageSyncQueueEntry? ImageSyncQueueEntry { get; set; }
    }
}
