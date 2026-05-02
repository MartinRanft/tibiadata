namespace TibiaDataApi.Services.Caching
{
    public static class CacheTags
    {
        public const string IpBans = "ip-bans";
        public const string ScraperQueries = "scraper-queries";
        public const string ApiStatistics = "api-statistics";
        public const string Assets = "assets";
        public const string Achievements = "achievements";
        public const string Bestiary = "bestiary";
        public const string Bosstiary = "bosstiary";
        public const string Books = "books";
        public const string Buildings = "buildings";
        public const string Categories = "categories";
        public const string Charms = "charms";
        public const string Corpses = "corpses";
        public const string Creatures = "creatures";
        public const string Effects = "effects";
        public const string HuntingPlaces = "hunting-places";
        public const string Items = "items";
        public const string Keys = "keys";
        public const string Locations = "locations";
        public const string LootStatistics = "loot-statistics";
        public const string Missiles = "missiles";
        public const string Mounts = "mounts";
        public const string Npcs = "npcs";
        public const string Objects = "objects";
        public const string Outfits = "outfits";
        public const string Quests = "quests";
        public const string Spells = "spells";
        public const string Streets = "streets";
        public const string WheelOfDestiny = "wheel-of-destiny";
        public const string WikiArticles = "wiki-articles";
        public const string WikiPages = "wiki-pages";

        public static IReadOnlyList<string> ScrapedContentTags { get; } =
        [
            Achievements,
            Assets,
            Bestiary,
            Bosstiary,
            Books,
            Buildings,
            Categories,
            Charms,
            Corpses,
            Creatures,
            Effects,
            HuntingPlaces,
            Items,
            Keys,
            Locations,
            LootStatistics,
            Missiles,
            Mounts,
            Npcs,
            Objects,
            Outfits,
            Quests,
            Spells,
            Streets,
            WheelOfDestiny,
            WikiArticles,
            WikiPages
        ];

        public static string IpBanAddress(string ipAddress)
        {
            return $"ip-ban:{ipAddress}";
        }

        public static string Category(string categorySlug)
        {
            return $"category:{categorySlug.Trim()}";
        }

        public static string Asset(int assetId)
        {
            return $"asset:{assetId}";
        }

        public static string AssetStorageKey(string storageKey)
        {
            return $"asset-storage:{storageKey.Trim()}";
        }

        public static string ScrapeLog(int scrapeLogId)
        {
            return $"scrape-log:{scrapeLogId}";
        }
    }
}
