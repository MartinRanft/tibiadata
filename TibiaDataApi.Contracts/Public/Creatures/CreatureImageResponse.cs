namespace TibiaDataApi.Contracts.Public.Creatures
{
    public sealed record CreatureImageResponse(
        int AssetId,
        string StorageKey,
        string FileName,
        string? MimeType,
        int? Width,
        int? Height);
}