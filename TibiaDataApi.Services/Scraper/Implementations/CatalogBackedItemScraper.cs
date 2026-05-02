using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Scraper.Parsing;
using TibiaDataApi.Services.Text;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Scraper.Implementations
{
    public partial class CatalogBackedItemScraper(
        string categorySlug,
        ITibiaWikiHttpService tibiaWikiHttpService,
        IItemImageSyncService itemImageSyncService,
        ILogger logger) : BaseScraper(tibiaWikiHttpService, itemImageSyncService, logger)
    {
        protected override string CategorySlug => categorySlug;

        protected override string ScraperName => $"{CategoryDefinition.Name.Replace(" ", string.Empty)}Scraper";

        private ItemCategoryParsingProfile ParsingProfile =>
        ItemCategoryParsingProfileCatalog.GetRequiredProfile(CategorySlug);

        protected override Item BuildItem(string title, string content, WikiCategory category)
        {
            string resolvedName = Extract(content, GetFieldAliases(ItemParsingFieldKeys.Name, "name"));
            string itemName = string.IsNullOrWhiteSpace(resolvedName) ? title : resolvedName;
            string? actualName = ExtractOptional(content, ItemParsingFieldKeys.ActualName, "actualname");

            Item item = new()
            {
                CategoryId = category.Id,
                Category = category,
                Name = itemName,
                NormalizedName = EntityNameNormalizer.Normalize(itemName),
                ActualName = actualName,
                NormalizedActualName = EntityNameNormalizer.NormalizeOptional(actualName),
                Plural = ExtractOptional(content, ItemParsingFieldKeys.Plural, "plural"),
                Article = ExtractOptional(content, ItemParsingFieldKeys.Article, "article"),
                Implemented = ExtractOptional(content, ItemParsingFieldKeys.Implemented, "implemented"),
                TemplateType = ExtractOptional(content, ItemParsingFieldKeys.TemplateType, "templatetype", "template") ?? "Object",
                WikiUrl = $"https://tibia.fandom.com/wiki/{Uri.EscapeDataString(title.Replace(" ", "_"))}",
                LastSeenAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,

                ItemId = ExtractProfileList(content, ItemParsingFieldKeys.ItemId, "itemid", "ids"),
                DroppedBy = ExtractTemplateList(content, "Dropped By"),
                Sounds = ExtractProfileList(content, ItemParsingFieldKeys.Sounds, "sounds", "sound"),

                ObjectClass = ExtractOptional(content, ItemParsingFieldKeys.ObjectClass, "objectclass") ?? category.ObjectClass,
                PrimaryType = ExtractOptional(content, ItemParsingFieldKeys.PrimaryType, "primarytype") ?? category.Name,
                SecondaryType = ExtractOptional(content, ItemParsingFieldKeys.SecondaryType, "secondarytype"),

                WeaponType = ExtractOptional(content, ItemParsingFieldKeys.WeaponType, "weapontype"),
                Hands = ExtractOptional(content, ItemParsingFieldKeys.Hands, "hands"),
                Attack = ResolveAttack(content),
                Defense = ExtractOptional(content, ItemParsingFieldKeys.Defense, "defense", "def"),
                DefenseMod = ExtractOptional(content, ItemParsingFieldKeys.DefenseMod, "defensemod", "defmod"),
                Armor = ExtractOptional(content, ItemParsingFieldKeys.Armor, "armor"),
                Range = ExtractOptional(content, ItemParsingFieldKeys.Range, "range"),
                LevelRequired = ExtractOptional(content, ItemParsingFieldKeys.LevelRequired, "levelrequired", "level", "lvl", "required_level", "minlevel"),
                ImbueSlots = ExtractOptional(content, ItemParsingFieldKeys.ImbueSlots, "imbueslots", "imbuing", "slots"),
                Vocation = ExtractOptional(content, ItemParsingFieldKeys.Vocation, "vocrequired", "voc", "vocation", "vocs"),

                DamageType = ExtractOptional(content, ItemParsingFieldKeys.DamageType, "damagetype", "damage_type"),
                DamageRange = ExtractOptional(content, ItemParsingFieldKeys.DamageRange, "damagerange", "damage_range"),
                EnergyAttack = ExtractOptional(content, ItemParsingFieldKeys.EnergyAttack, "energy_attack", "energy"),
                FireAttack = ExtractOptional(content, ItemParsingFieldKeys.FireAttack, "fire_attack", "fire"),
                EarthAttack = ExtractOptional(content, ItemParsingFieldKeys.EarthAttack, "earth_attack", "earth"),
                IceAttack = ExtractOptional(content, ItemParsingFieldKeys.IceAttack, "ice_attack", "ice"),
                DeathAttack = ExtractOptional(content, ItemParsingFieldKeys.DeathAttack, "death_attack", "death"),
                HolyAttack = ExtractOptional(content, ItemParsingFieldKeys.HolyAttack, "holy_attack", "holy"),

                Stackable = ExtractOptional(content, ItemParsingFieldKeys.Stackable, "stackable"),
                Usable = ExtractOptional(content, ItemParsingFieldKeys.Usable, "usable"),
                Marketable = ExtractOptional(content, ItemParsingFieldKeys.Marketable, "marketable"),
                Walkable = ExtractOptional(content, ItemParsingFieldKeys.Walkable, "walkable"),

                NpcPrice = ExtractOptional(content, ItemParsingFieldKeys.NpcPrice, "npcprice", "npc_price"),
                NpcValue = ExtractOptional(content, ItemParsingFieldKeys.NpcValue, "npcvalue", "npc_value"),
                Value = ExtractOptional(content, ItemParsingFieldKeys.Value, "value", "marketvalue"),

                Weight = ExtractOptional(content, ItemParsingFieldKeys.Weight, "weight", "oz"),
                Attrib = ExtractOptional(content, ItemParsingFieldKeys.Attrib, "attrib", "attributes"),
                UpgradeClass = ExtractOptional(content, ItemParsingFieldKeys.UpgradeClass, "upgradeclass", "upgradeclassification", "upgrade"),
                AdditionalAttributesJson = BuildAdditionalAttributesJson(content)
            };

            return item;
        }

        private string? BuildAdditionalAttributesJson(string content)
        {
            Dictionary<string, string> additionalAttributes = new(StringComparer.OrdinalIgnoreCase);
            IReadOnlyDictionary<string, string[]> aliasesByKey =
            ParsingProfile.MergeAdditionalAttributeAliases(ItemCategoryParsingProfileCatalog.CommonAdditionalAttributeAliases);

            foreach((string key, string[] aliases) in aliasesByKey)
            {
                string value = Extract(content, aliases);

                if(!string.IsNullOrWhiteSpace(value))
                {
                    additionalAttributes[key] = value;
                }
            }

            return additionalAttributes.Count == 0
            ? null
            : JsonSerializer.Serialize(additionalAttributes);
        }

        private string? ResolveAttack(string content)
        {
            string directAttack = Extract(content, GetFieldAliases(ItemParsingFieldKeys.Attack, "attack", "atk"));
            if(!string.IsNullOrWhiteSpace(directAttack))
            {
                return NullIfEmpty(directAttack);
            }

            string attributes = Extract(content, GetFieldAliases(ItemParsingFieldKeys.Attrib, "attrib", "attributes"));
            if(string.IsNullOrWhiteSpace(attributes))
            {
                return null;
            }

            Match match = AttackFallbackRegex().Match(attributes);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string? NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private string? ExtractOptional(string content, string fieldKey, params string[] defaultAliases)
        {
            return NullIfEmpty(Extract(content, GetFieldAliases(fieldKey, defaultAliases)));
        }

        private List<string> ExtractProfileList(string content, string fieldKey, params string[] defaultAliases)
        {
            return ExtractList(content, GetFieldAliases(fieldKey, defaultAliases));
        }

        private string[] GetFieldAliases(string fieldKey, params string[] defaultAliases)
        {
            return ParsingProfile.GetFieldAliases(fieldKey, defaultAliases);
        }

        [GeneratedRegex(@"Atk:?\s*(\d+)", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex AttackFallbackRegex();
    }
}