using TibiaDataApi.Services.Entities.Items;

namespace TibiaDataApi.Services.Entities.Assets
{
    public class ItemAsset
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        public Item? Item { get; set; }

        public int AssetId { get; set; }

        public Asset? Asset { get; set; }

        public AssetKind AssetKind { get; set; }

        public int SortOrder { get; set; }

        public bool IsPrimary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}