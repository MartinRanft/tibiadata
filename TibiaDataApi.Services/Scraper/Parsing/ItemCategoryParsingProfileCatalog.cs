using TibiaDataApi.Services.Categories;

namespace TibiaDataApi.Services.Scraper.Parsing
{
    public static class ItemCategoryParsingProfileCatalog
    {
        private static readonly IReadOnlyDictionary<string, string[]> EmptyFieldAliasExtensions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, string[]> CommonAdditionalAttributeAliases { get; } =
            BuildCommonAdditionalAttributeAliases();

        public static IReadOnlyDictionary<string, ItemCategoryParsingProfile> All { get; } = BuildProfiles();

        public static ItemCategoryParsingProfile GetRequiredProfile(string categorySlug)
        {
            if(!All.TryGetValue(categorySlug, out ItemCategoryParsingProfile? profile))
            {
                throw new InvalidOperationException(
                    $"No item parsing profile was found for category slug '{categorySlug}'.");
            }

            return profile;
        }

        public static IReadOnlyList<string> GetMissingItemCategorySlugs()
        {
            HashSet<string> itemCategorySlugs = TibiaWikiCategoryCatalog.All
                                                                        .Where(entry => entry.ContentType == WikiContentType.Item)
                                                                        .Select(entry => entry.Slug)
                                                                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return itemCategorySlugs
                   .Where(slug => !All.ContainsKey(slug))
                   .OrderBy(slug => slug, StringComparer.OrdinalIgnoreCase)
                   .ToList();
        }

        private static IReadOnlyDictionary<string, ItemCategoryParsingProfile> BuildProfiles()
        {
            Dictionary<string, ItemCategoryParsingProfile> profiles = new(StringComparer.OrdinalIgnoreCase);

            AddProfile(
                profiles,
                "body-equipment",
                CreateProfile(
                    "BodyEquipment",
                    CreateFieldAliases(
                        (ItemParsingFieldKeys.Defense, ["shielding", "defvalue"]),
                        (ItemParsingFieldKeys.Armor, ["arm", "armorvalue"])),
                    CreateAdditionalAliases(
                        ("slotPosition", ["slotposition"]),
                        ("resistAll", ["allresist", "all_resist"]),
                        ("speedBonus", ["speed", "speedbonus"]),
                        ("magicLevelBonus", ["magiclevel", "magic_level", "ml"]),
                        ("healingMagicLevelBonus", ["healingmagiclevel", "healing_magic_level"]),
                        ("shieldingBonus", ["shielding", "shieldingbonus"]),
                        ("distanceBonus", ["distance", "distancebonus"]),
                        ("skillBonuses", ["skills", "skillbonus", "skillbonuses"]))),
                ["helmets", "armors", "shields", "legs", "spellbooks", "boots", "quivers"]);

            AddProfile(
                profiles,
                "melee-weapons",
                CreateProfile(
                    "MeleeWeapons",
                    CreateFieldAliases(
                        (ItemParsingFieldKeys.Attack, ["meleeattack", "attackvalue"]),
                        (ItemParsingFieldKeys.Defense, ["weapondefense", "weapon_defense"]),
                        (ItemParsingFieldKeys.DefenseMod, ["defmod", "defensemodifier"])),
                    CreateAdditionalAliases(
                        ("criticalHitChance", ["crithit", "criticalhitchance"]),
                        ("criticalExtraDamage", ["critdamage", "criticalextradamage"]),
                        ("hitChance", ["hitchance", "hit_chance"]),
                        ("elementalBonus", ["elementalbonus", "element_bonus"]),
                        ("twoHanded", ["twohanded", "two_handed"]))),
                ["axe-weapons", "club-weapons", "sword-weapons", "fist-fighting-weapons"]);

            AddProfile(
                profiles,
                "magic-weapons",
                CreateProfile(
                    "MagicWeapons",
                    CreateFieldAliases(
                        (ItemParsingFieldKeys.Attack, ["wandattack", "rodattack"]),
                        (ItemParsingFieldKeys.DamageType, ["element", "elementtype"])),
                    CreateAdditionalAliases(
                        ("manaCost", ["manacost", "mana"]),
                        ("magicLevelBonus", ["magiclevel", "magic_level", "ml"]),
                        ("healingMagicLevelBonus", ["healingmagiclevel", "healing_magic_level"]),
                        ("charges", ["charges", "chargecount"]),
                        ("cooldown", ["cooldown", "cooldowntime"]),
                        ("resistPercent", ["resist", "resistpercent"]))),
                ["rods", "wands", "old-wands"]);

            AddProfile(
                profiles,
                "distance-weapons",
                CreateProfile(
                    "DistanceWeapons",
                    CreateFieldAliases(
                        (ItemParsingFieldKeys.Attack, ["distanceattack", "attackvalue"]),
                        (ItemParsingFieldKeys.Range, ["shootrange", "rangevalue"])),
                    CreateAdditionalAliases(
                        ("hitChance", ["hitchance", "hit_chance"]),
                        ("breakChance", ["breakchance", "break_chance"]),
                        ("ammoType", ["ammotype", "ammo_type"]),
                        ("rangeBonus", ["rangebonus", "range_bonus"]))),
                ["throwing-weapons", "bows", "crossbows"]);

            AddProfile(
                profiles,
                "ammunition",
                CreateProfile(
                    "Ammunition",
                    CreateFieldAliases(
                        (ItemParsingFieldKeys.Attack, ["distanceattack", "ammoattack", "attackvalue"]),
                        (ItemParsingFieldKeys.DamageType, ["element", "elementtype"])),
                    CreateAdditionalAliases(
                        ("ammoType", ["ammotype", "ammo_type"]),
                        ("breakChance", ["breakchance", "break_chance"]),
                        ("hitChance", ["hitchance", "hit_chance"]),
                        ("requiredWeapon", ["requiredweapon", "required_weapon"]),
                        ("elementDamage", ["elementaldamage", "element_damage"]))),
                ["bow-ammunition", "crossbow-ammunition"]);

            AddProfile(
                profiles,
                "books-and-documents",
                CreateProfile(
                    "BooksAndDocuments",
                    EmptyFieldAliasExtensions,
                    CreateAdditionalAliases(
                        ("text", ["text", "booktext", "content"]),
                        ("author", ["author", "writtenby"]),
                        ("pages", ["pages", "pagecount"]),
                        ("location", ["location", "foundat"]))),
                ["books", "documents-and-papers"]);

            AddProfile(
                profiles,
                "containers",
                CreateProfile(
                    "Containers",
                    EmptyFieldAliasExtensions,
                    CreateAdditionalAliases(
                        ("capacity", ["capacity", "slots", "volume"]),
                        ("containerType", ["containertype", "container_type"]),
                        ("liquidContainer", ["liquidcontainer", "liquid_container"]),
                        ("hangable", ["hangable"]),
                        ("moveable", ["moveable", "movable"]))),
                ["containers"]);

            AddProfile(
                profiles,
                "decorations",
                CreateProfile(
                    "Decorations",
                    EmptyFieldAliasExtensions,
                    CreateAdditionalAliases(
                        ("hangable", ["hangable"]),
                        ("moveable", ["moveable", "movable"]),
                        ("houseUse", ["houseuse", "house_use"]),
                        ("eventSource", ["event", "eventsource"]))),
                [
                    "carpets",
                    "contest-prizes",
                    "fansite-items",
                    "decorations",
                    "dolls-and-bears",
                    "furniture",
                    "musical-instruments",
                    "trophies",
                    "party-items"
                ]);

            AddProfile(
                profiles,
                "consumables",
                CreateProfile(
                    "Consumables",
                    CreateFieldAliases(
                        (ItemParsingFieldKeys.Value, ["marketvalue", "value"])),
                    CreateAdditionalAliases(
                        ("nutrition", ["nutrition", "foodvalue"]),
                        ("fluidType", ["fluidtype", "fluid_type"]),
                        ("drunk", ["drunk", "drunkenness"]),
                        ("healAmount", ["heal", "healing", "healamount"]),
                        ("regeneration", ["regeneration", "regen"]),
                        ("charges", ["charges"]))),
                ["creature-products", "food", "liquids", "plants-and-herbs"]);

            AddProfile(
                profiles,
                "accessories",
                CreateProfile(
                    "Accessories",
                    CreateFieldAliases(
                        (ItemParsingFieldKeys.LevelRequired, ["requiredlevel", "minlevel"]),
                        (ItemParsingFieldKeys.Vocation, ["vocrequired", "vocationrequired"])),
                    CreateAdditionalAliases(
                        ("charges", ["charges"]),
                        ("duration", ["duration", "effectduration"]),
                        ("speedBonus", ["speed", "speedbonus"]),
                        ("resistPercent", ["resist", "resistpercent"]),
                        ("skillBonuses", ["skills", "skillbonus", "skillbonuses"]))),
                ["amulets-and-necklaces", "rings", "clothing-accessories"]);

            AddProfile(
                profiles,
                "keys",
                CreateProfile(
                    "Keys",
                    EmptyFieldAliasExtensions,
                    CreateAdditionalAliases(
                        ("keyNumber", ["keynumber", "key_number"]),
                        ("doorLevel", ["doorlevel", "door_level"]),
                        ("usage", ["usage", "usedfor"]))),
                ["keys"]);

            AddProfile(
                profiles,
                "light-sources",
                CreateProfile(
                    "LightSources",
                    EmptyFieldAliasExtensions,
                    CreateAdditionalAliases(
                        ("lightRadius", ["lightradius", "light_radius"]),
                        ("lightColor", ["lightcolor", "light_color"]),
                        ("duration", ["duration", "burntime"]),
                        ("charges", ["charges"]))),
                ["light-sources"]);

            AddProfile(
                profiles,
                "utility-items",
                CreateProfile(
                    "UtilityItems",
                    EmptyFieldAliasExtensions,
                    CreateAdditionalAliases(
                        ("charges", ["charges"]),
                        ("duration", ["duration", "effectduration"]),
                        ("toolType", ["tooltype", "tool_type"]),
                        ("teleportTo", ["teleportto", "destination"]),
                        ("capacity", ["capacity", "volume"]),
                        ("paintColor", ["paintcolor", "paint_color"]))),
                ["painting-equipment", "tools", "kitchen-tools", "taming-items", "diving-equipment"]);

            AddProfile(
                profiles,
                "magical-misc",
                CreateProfile(
                    "MagicalMisc",
                    CreateFieldAliases(
                        (ItemParsingFieldKeys.DamageType, ["element", "elementtype"]),
                        (ItemParsingFieldKeys.LevelRequired, ["requiredlevel", "minlevel"])),
                    CreateAdditionalAliases(
                        ("charges", ["charges"]),
                        ("manaCost", ["manacost", "mana"]),
                        ("spellName", ["spell", "spellname"]),
                        ("effect", ["effect", "effects"]),
                        ("cooldown", ["cooldown", "cooldowntime"]),
                        ("uses", ["uses", "usage"]))),
                ["enchanted-items", "game-tokens", "magical-items", "blessing-charms", "quest-items", "runes"]);

            AddProfile(
                profiles,
                "valuables",
                CreateProfile(
                    "Valuables",
                    EmptyFieldAliasExtensions,
                    CreateAdditionalAliases(
                        ("material", ["material", "madeof"]),
                        ("rarity", ["rarity"]),
                        ("eventSource", ["event", "eventsource"]),
                        ("stackSize", ["stacksize", "stack_size"]))),
                ["valuables", "metals"]);

            AddProfile(
                profiles,
                "generic-misc",
                CreateProfile(
                    "GenericMisc",
                    EmptyFieldAliasExtensions,
                    CreateAdditionalAliases(
                        ("moveable", ["moveable", "movable"]),
                        ("usage", ["usage"]),
                        ("flavorText", ["flavortext", "flavor_text"]))),
                ["rubbish"]);

            return profiles;
        }

        private static ItemCategoryParsingProfile CreateProfile(
            string key,
            IReadOnlyDictionary<string, string[]> fieldAliasExtensions,
            IReadOnlyDictionary<string, string[]> additionalAttributeAliases)
        {
            return new ItemCategoryParsingProfile
            {
                Key = key,
                FieldAliasExtensions = fieldAliasExtensions,
                AdditionalAttributeAliases = additionalAttributeAliases
            };
        }

        private static void AddProfile(
            IDictionary<string, ItemCategoryParsingProfile> profiles,
            string registrationKey,
            ItemCategoryParsingProfile profile,
            IReadOnlyList<string> categorySlugs)
        {
            foreach(string categorySlug in categorySlugs)
            {
                if(profiles.ContainsKey(categorySlug))
                {
                    throw new InvalidOperationException(
                        $"The parsing profile registration '{registrationKey}' attempted to register '{categorySlug}' more than once.");
                }

                profiles[categorySlug] = profile;
            }
        }

        private static IReadOnlyDictionary<string, string[]> BuildCommonAdditionalAttributeAliases()
        {
            return CreateAdditionalAliases(
                ("slot", ["slot"]),
                ("pickupable", ["pickupable"]),
                ("immobile", ["immobile"]),
                ("buyFrom", ["buyfrom"]),
                ("sellTo", ["sellto"]),
                ("notes", ["notes"]),
                ("notes2", ["notes2"]),
                ("history", ["history"]),
                ("resist", ["resist"]),
                ("lightRadius", ["lightradius"]),
                ("lightColor", ["lightcolor"]),
                ("manaCost", ["manacost"]),
                ("attackModifier", ["atk_mod"]),
                ("hitModifier", ["hit_mod"]),
                ("criticalHitChance", ["crithit_ch"]),
                ("criticalExtraDamage", ["critextra_dmg"]),
                ("pickupSound", ["sound", "sounds"]));
        }

        private static IReadOnlyDictionary<string, string[]> CreateFieldAliases(
            params (string Key, string[] Aliases)[] entries)
        {
            return entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Aliases
                              .Where(alias => !string.IsNullOrWhiteSpace(alias))
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<string, string[]> CreateAdditionalAliases(
            params (string Key, string[] Aliases)[] entries)
        {
            return entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Aliases
                              .Where(alias => !string.IsNullOrWhiteSpace(alias))
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}