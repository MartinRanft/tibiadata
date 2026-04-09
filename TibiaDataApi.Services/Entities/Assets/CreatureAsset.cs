using TibiaDataApi.Services.Entities.Creatures;

namespace TibiaDataApi.Services.Entities.Assets
{
    public class CreatureAsset
    {
        public int Id { get; set; }

        public int CreatureId { get; set; }

        public Creature? Creature { get; set; }

        public int AssetId { get; set; }

        public Asset? Asset { get; set; }

        public AssetKind AssetKind { get; set; }

        public int SortOrder { get; set; }

        public bool IsPrimary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}