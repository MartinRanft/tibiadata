namespace TibiaDataApi.Services.Assets
{
    public sealed class AssetStorageOptions
    {
        public const string SectionName = "Assets";

        public string StorageRootPath { get; set; } = "data/assets";

        public bool DownloadItemImages { get; set; } = true;

        public string ItemImageDirectory { get; set; } = "items";

        public bool DownloadCreatureImages { get; set; } = true;

        public string CreatureImageDirectory { get; set; } = "creatures";
    }
}