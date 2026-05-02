namespace TibiaDataApi.Services.Entities.Assets
{
    public class Asset
    {
        public int Id { get; set; }

        public required string StorageKey { get; set; }

        public required string FileName { get; set; }

        public string? SourcePageTitle { get; set; }

        public string? SourceFileTitle { get; set; }

        public string? SourceUrl { get; set; }

        public string? MimeType { get; set; }

        public string? Extension { get; set; }

        public long SizeBytes { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public string? SourceSha1 { get; set; }

        public string? ContentMd5 { get; set; }

        public string? ContentSha256 { get; set; }

        public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<ItemAsset> ItemAssets { get; set; } = [];

        public List<CreatureAsset> CreatureAssets { get; set; } = [];
    }
}
