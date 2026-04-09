namespace TibiaDataApi.Services.Categories
{
    public static class TibiaWikiCategoryCatalog
    {
        public static IReadOnlyList<WikiCategoryDefinition> All { get; } = BuildDefinitions();

        public static WikiCategoryDefinition GetRequiredDefinition(WikiContentType contentType, string slug)
        {
            WikiCategoryDefinition? definition = All.FirstOrDefault(entry =>
            entry.ContentType == contentType &&
            string.Equals(entry.Slug, slug, StringComparison.OrdinalIgnoreCase));

            return definition ?? throw new InvalidOperationException(
                $"No wiki category definition was found for content type '{contentType}' and slug '{slug}'.");
        }

        private static IReadOnlyList<WikiCategoryDefinition> BuildDefinitions()
        {
            List<WikiCategoryDefinition> definitions = new();
            int sortOrder = 0;

            AddCategoryGroup(definitions,
                WikiContentType.Item,
                "body-equipment",
                "Body Equipment",
                "Equipment",
                ref sortOrder,
                [
                    ("helmets", "Helmets", "Category:Helmets"),
                    ("armors", "Armors", "Category:Armors"),
                    ("shields", "Shields", "Category:Shields"),
                    ("legs", "Legs", "Category:Legs"),
                    ("spellbooks", "Spellbooks", "Category:Spellbooks"),
                    ("boots", "Boots", "Category:Boots"),
                    ("quivers", "Quivers", "Category:Quivers")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Item,
                "weapons",
                "Weapons",
                "Weapon",
                ref sortOrder,
                [
                    ("axe-weapons", "Axe Weapons", "Category:Axe Weapons"),
                    ("club-weapons", "Club Weapons", "Category:Club Weapons"),
                    ("sword-weapons", "Sword Weapons", "Category:Sword Weapons"),
                    ("fist-fighting-weapons", "Fist Fighting Weapons", "Category:Fist Fighting Weapons"),
                    ("rods", "Rods", "Category:Rods"),
                    ("wands", "Wands", "Category:Wands"),
                    ("throwing-weapons", "Throwing Weapons", "Category:Throwing Weapons"),
                    ("bows", "Bows", "Category:Bows"),
                    ("crossbows", "Crossbows", "Category:Crossbows")
                ]);

            AddPageGroup(definitions,
                WikiContentType.Item,
                "weapons",
                "Weapons",
                "Weapon",
                ref sortOrder,
                [
                    ("bow-ammunition", "Bow Ammunition", "Bow Ammunition"),
                    ("crossbow-ammunition", "Crossbow Ammunition", "Crossbow Ammunition"),
                    ("old-wands", "Old Wands (deprecated)", "Old Wands and Rods")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Item,
                "household-items",
                "Household Items",
                "HouseholdItem",
                ref sortOrder,
                [
                    ("books", "Books", "Category:Books"),
                    ("carpets", "Carpets", "Category:Carpets"),
                    ("containers", "Containers", "Category:Containers"),
                    ("contest-prizes", "Contest Prizes", "Category:Contest Prizes"),
                    ("fansite-items", "Fansite Items", "Category:Fansite Items"),
                    ("decorations", "Decorations", "Category:Decorations"),
                    ("documents-and-papers", "Documents and Papers", "Category:Documents and Papers"),
                    ("dolls-and-bears", "Dolls and Bears", "Category:Dolls and Bears"),
                    ("furniture", "Furniture", "Category:Furniture"),
                    ("kitchen-tools", "Kitchen Tools", "Category:Kitchen Tools"),
                    ("musical-instruments", "Musical Instruments", "Category:Musical Instruments"),
                    ("trophies", "Trophies", "Category:Trophies")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Item,
                "plants-animal-products-food-and-drink",
                "Plants, Animal Products, Food and Drink",
                "Consumable",
                ref sortOrder,
                [
                    ("creature-products", "Creature Products", "Category:Creature Products"),
                    ("food", "Food", "Category:Food"),
                    ("liquids", "Liquids", "Category:Liquids"),
                    ("plants-and-herbs", "Plants and Herbs", "Category:Plants and Herbs")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Item,
                "tools-and-other-equipment",
                "Tools and Other Equipment",
                "Utility",
                ref sortOrder,
                [
                    ("amulets-and-necklaces", "Amulets and Necklaces", "Category:Amulets and Necklaces"),
                    ("keys", "Keys", "Category:Keys"),
                    ("light-sources", "Light Sources", "Category:Light Sources"),
                    ("painting-equipment", "Painting Equipment", "Category:Painting Equipment"),
                    ("rings", "Rings", "Category:Rings"),
                    ("tools", "Tools", "Category:Tools"),
                    ("taming-items", "Taming Items", "Category:Taming Items"),
                    ("diving-equipment", "Diving Equipment", "Category:Diving Equipment")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Item,
                "other-items",
                "Other Items",
                "OtherItem",
                ref sortOrder,
                [
                    ("clothing-accessories", "Clothing Accessories", "Category:Clothing Accessories"),
                    ("enchanted-items", "Enchanted Items", "Category:Enchanted Items"),
                    ("game-tokens", "Game Tokens", "Category:Game Tokens"),
                    ("valuables", "Valuables", "Category:Valuables"),
                    ("magical-items", "Magical Items", "Category:Magical Items"),
                    ("metals", "Metals", "Category:Metals"),
                    ("party-items", "Party Items", "Category:Party Items"),
                    ("blessing-charms", "Blessing Charms", "Category:Blessing Charms"),
                    ("quest-items", "Quest Items", "Category:Quest Items"),
                    ("rubbish", "Rubbish", "Category:Rubbish"),
                    ("runes", "Runes", "Category:Runes")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.BookText,
                "libraries-and-documents",
                "Libraries and Documents",
                "BookText",
                ref sortOrder,
                [
                    ("book-texts", "Book Texts", "Category:Book Texts")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Quest,
                "quest-documentation",
                "Quest Documentation",
                "Quest",
                ref sortOrder,
                [
                    ("quests-in-game-quest-log", "Quests in In-Game Quest Log", "Category:Quests in In-Game Quest Log"),
                    ("quest-overview-pages", "Quest Overview Pages", "Category:Quest Overview Pages"),
                    ("quest-spoiling-pages", "Quest Spoiling Pages", "Category:Quest Spoiling Pages"),
                    ("quest-transcripts", "Quest Transcripts", "Category:Quest Transcripts"),
                    ("quests-with-transcripts", "Quest with Transcripts", "Category:Quest with Transcripts")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Quest,
                "quest-access-and-location",
                "Quest Access and Location",
                "Quest",
                ref sortOrder,
                [
                    ("free-account-quests", "Free Account Quests", "Category:Free Account Quests"),
                    ("partially-premium-quests", "Partially Premium Quests", "Category:Partially Premium Quests"),
                    ("premium-quests", "Premium Quests", "Category:Premium Quests"),
                    ("mainland-quests", "Mainland Quests", "Category:Mainland Quests"),
                    ("rookgaard-quests", "Rookgaard Quests", "Category:Rookgaard Quests")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Quest,
                "quest-types-and-storylines",
                "Quest Types and Storylines",
                "Quest",
                ref sortOrder,
                [
                    ("addon-quests", "Addon Quests", "Category:Addon Quests"),
                    ("friends-and-traders", "Friends and Traders", "Category:Friends and Traders"),
                    ("outfit-quests", "Outfit Quests", "Category:Outfit Quests"),
                    ("tibia-tales", "Tibia Tales", "Category:Tibia Tales"),
                    ("the-new-frontier-quest", "The New Frontier Quest", "Category:The New Frontier Quest"),
                    ("the-ultimate-challenges", "The Ultimate Challenges", "Category:The Ultimate Challenges")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.HuntingPlace,
                "hunting-places",
                "Hunting Places",
                "HuntingPlace",
                ref sortOrder,
                [
                    ("hunting-places", "Hunting Places", "Category:Hunting Places")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Creature,
                "creatures",
                "Creatures",
                "Creature",
                ref sortOrder,
                [
                    ("creatures", "Creatures", "Category:Creatures")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Achievement,
                "achievements",
                "Achievements",
                "Achievement",
                ref sortOrder,
                [
                    ("achievements", "Achievements", "Category:Achievements")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Building,
                "buildings",
                "Buildings",
                "Building",
                ref sortOrder,
                [
                    ("buildings", "Buildings", "Category:Buildings")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Charm,
                "charms",
                "Charms",
                "Charm",
                ref sortOrder,
                [
                    ("charms", "Charms", "Category:Charms")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Corpse,
                "corpses",
                "Corpses",
                "Corpse",
                ref sortOrder,
                [
                    ("corpses", "Corpses", "Category:Corpses")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Effect,
                "effects",
                "Effects",
                "Effect",
                ref sortOrder,
                [
                    ("effects", "Effects", "Category:Effects")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Location,
                "locations",
                "Locations",
                "Location",
                ref sortOrder,
                [
                    ("locations", "Locations", "Category:Locations")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.LootStatistic,
                "loot-statistics",
                "Loot Statistics",
                "LootStatistic",
                ref sortOrder,
                [
                    ("loot-statistics", "Loot Statistics", "Category:Loot Statistics")
                ],
                WikiCategorySourceKind.CategoryMembersWithNamespace);

            AddCategoryGroup(definitions,
                WikiContentType.Missile,
                "missiles",
                "Missiles",
                "Missile",
                ref sortOrder,
                [
                    ("missiles", "Missiles", "Category:Missiles")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Mount,
                "mounts",
                "Mounts",
                "Mount",
                ref sortOrder,
                [
                    ("mounts", "Mounts", "Category:Mounts")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Npc,
                "npcs",
                "NPCs",
                "Npc",
                ref sortOrder,
                [
                    ("npcs", "NPCs", "Category:NPCs")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Object,
                "objects",
                "Objects",
                "Object",
                ref sortOrder,
                [
                    ("objects", "Objects", "Category:Objects")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Outfit,
                "outfits",
                "Outfits",
                "Outfit",
                ref sortOrder,
                [
                    ("outfits", "Outfits", "Category:Outfits")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Spell,
                "spells",
                "Spells",
                "Spell",
                ref sortOrder,
                [
                    ("spells", "Spells", "Category:Spells")
                ]);

            AddCategoryGroup(definitions,
                WikiContentType.Street,
                "streets",
                "Streets",
                "Street",
                ref sortOrder,
                [
                    ("streets", "Streets", "Category:Streets")
                ]);

            AddAllPagesGroup(definitions,
                WikiContentType.WikiPage,
                "wiki-pages",
                "Wiki Pages",
                "WikiPage",
                ref sortOrder,
                [
                    ("wiki-pages", "Wiki Pages", string.Empty)
                ]);

            return definitions;
        }

        private static void AddCategoryGroup(
            ICollection<WikiCategoryDefinition> definitions,
            WikiContentType contentType,
            string groupSlug,
            string groupName,
            string objectClass,
            ref int sortOrder,
            IReadOnlyList<(string Slug, string Name, string SourceTitle)> entries,
            WikiCategorySourceKind sourceKind = WikiCategorySourceKind.CategoryMembers)
        {
            foreach((string slug, string name, string sourceTitle) in entries)
            {
                definitions.Add(new WikiCategoryDefinition(
                    slug,
                    name,
                    contentType,
                    groupSlug,
                    groupName,
                    sourceKind,
                    sourceTitle,
                    null,
                    objectClass,
                    ++sortOrder));
            }
        }

        private static void AddPageGroup(
            ICollection<WikiCategoryDefinition> definitions,
            WikiContentType contentType,
            string groupSlug,
            string groupName,
            string objectClass,
            ref int sortOrder,
            IReadOnlyList<(string Slug, string Name, string SourceTitle)> entries)
        {
            foreach((string slug, string name, string sourceTitle) in entries)
            {
                definitions.Add(new WikiCategoryDefinition(
                    slug,
                    name,
                    contentType,
                    groupSlug,
                    groupName,
                    WikiCategorySourceKind.WikiPage,
                    sourceTitle,
                    null,
                    objectClass,
                    ++sortOrder));
            }
        }

        private static void AddAllPagesGroup(
            ICollection<WikiCategoryDefinition> definitions,
            WikiContentType contentType,
            string groupSlug,
            string groupName,
            string objectClass,
            ref int sortOrder,
            IReadOnlyList<(string Slug, string Name, string SourceTitle)> entries)
        {
            foreach((string slug, string name, string sourceTitle) in entries)
            {
                definitions.Add(new WikiCategoryDefinition(
                    slug,
                    name,
                    contentType,
                    groupSlug,
                    groupName,
                    WikiCategorySourceKind.AllPages,
                    sourceTitle,
                    null,
                    objectClass,
                    ++sortOrder));
            }
        }
    }
}