using TibiaDataApi.Services.Entities.Items;

namespace TibiaDataApi.Services.Scraper.Parsing
{
    public static class ItemParsingFieldKeys
    {
        public const string Name = nameof(Item.Name);
        public const string ActualName = nameof(Item.ActualName);
        public const string Plural = nameof(Item.Plural);
        public const string Article = nameof(Item.Article);
        public const string Implemented = nameof(Item.Implemented);
        public const string TemplateType = nameof(Item.TemplateType);
        public const string ItemId = nameof(Item.ItemId);
        public const string DroppedBy = nameof(Item.DroppedBy);
        public const string Sounds = nameof(Item.Sounds);
        public const string ObjectClass = nameof(Item.ObjectClass);
        public const string PrimaryType = nameof(Item.PrimaryType);
        public const string SecondaryType = nameof(Item.SecondaryType);
        public const string WeaponType = nameof(Item.WeaponType);
        public const string Hands = nameof(Item.Hands);
        public const string Attack = nameof(Item.Attack);
        public const string Defense = nameof(Item.Defense);
        public const string DefenseMod = nameof(Item.DefenseMod);
        public const string Armor = nameof(Item.Armor);
        public const string Range = nameof(Item.Range);
        public const string LevelRequired = nameof(Item.LevelRequired);
        public const string ImbueSlots = nameof(Item.ImbueSlots);
        public const string Vocation = nameof(Item.Vocation);
        public const string DamageType = nameof(Item.DamageType);
        public const string DamageRange = nameof(Item.DamageRange);
        public const string EnergyAttack = nameof(Item.EnergyAttack);
        public const string FireAttack = nameof(Item.FireAttack);
        public const string EarthAttack = nameof(Item.EarthAttack);
        public const string IceAttack = nameof(Item.IceAttack);
        public const string DeathAttack = nameof(Item.DeathAttack);
        public const string HolyAttack = nameof(Item.HolyAttack);
        public const string Stackable = nameof(Item.Stackable);
        public const string Usable = nameof(Item.Usable);
        public const string Marketable = nameof(Item.Marketable);
        public const string Walkable = nameof(Item.Walkable);
        public const string NpcPrice = nameof(Item.NpcPrice);
        public const string NpcValue = nameof(Item.NpcValue);
        public const string Value = nameof(Item.Value);
        public const string Weight = nameof(Item.Weight);
        public const string Attrib = nameof(Item.Attrib);
        public const string UpgradeClass = nameof(Item.UpgradeClass);
    }
}