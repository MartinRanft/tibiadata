namespace TibiaDataApi.Contracts.Public.Assets
{
    public sealed record AssetMetadataResponse
    {
        public int AssetId { get; init; }

        public string FileName { get; init; } = null!;

        public string MimeType { get; init; } = null!;

        public long Size { get; init; }

        public int? Width { get; init; }

        public int? Height { get; init; }

        public string? Hash { get; init; }
    }
}