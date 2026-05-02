namespace TibiaDataApi.Contracts.Public.Items
{
    public sealed record ItemImageResponse(
        int AssetId,
        string StorageKey,
        string FileName,
        string? MimeType,
        int? Width,
        int? Height);
}