using TibiaDataApi.Services.Entities.Assets;
using TibiaDataApi.Services.Entities.Categories;

namespace TibiaDataApi.Services.Entities.Items
{
    public class Item
    {
        public int Id { get; set; } 

        public int? CategoryId { get; set; }

        public WikiCategory? Category { get; set; }

        
        public required string Name { get; set; }

        public required string NormalizedName { get; set; }

        public string? ActualName { get; set; }

        public string? NormalizedActualName { get; set; }

        public string? Plural { get; set; }

        public string? Article { get; set; }

        public string? Implemented { get; set; }

        
        public List<string> ItemId { get; set; } = new();

        public List<string> DroppedBy { get; set; } = new();

        public List<string> Sounds { get; set; } = new();

        
        public string? TemplateType { get; set; }

        public string? ObjectClass { get; set; }

        public string? PrimaryType { get; set; }

        public string? SecondaryType { get; set; }

        
        public string? WeaponType { get; set; }

        public string? Hands { get; set; }

        public string? Attack { get; set; }

        public string? Defense { get; set; }

        public string? DefenseMod { get; set; }

        public string? Armor { get; set; }

        public string? Range { get; set; }

        public string? LevelRequired { get; set; }

        public string? ImbueSlots { get; set; }

        public string? Vocation { get; set; }


        
        public string? DamageType { get; set; }

        public string? DamageRange { get; set; }

        public string? EnergyAttack { get; set; }

        public string? FireAttack { get; set; }

        public string? EarthAttack { get; set; }

        public string? IceAttack { get; set; }

        public string? DeathAttack { get; set; }

        public string? HolyAttack { get; set; }

        
        public string? Stackable { get; set; }

        public string? Usable { get; set; }

        public string? Marketable { get; set; }

        public string? Walkable { get; set; }

        
        public string? NpcPrice { get; set; }

        public string? NpcValue { get; set; }

        public string? Value { get; set; } 

        
        public string? Weight { get; set; }

        public string? Attrib { get; set; } 

        public string? UpgradeClass { get; set; }

        
        public string? WikiUrl { get; set; }

        public string? AdditionalAttributesJson { get; set; }

        public DateTime? LastSeenAt { get; set; }

        public bool IsMissingFromSource { get; set; }

        public DateTime? MissingSince { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public List<ItemAsset> ItemAssets { get; set; } = [];

        public ItemImageSyncQueueEntry? ImageSyncQueueEntry { get; set; }
    }
}