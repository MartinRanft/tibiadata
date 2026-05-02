namespace TibiaDataApi.Contracts.Public.Items
{
    public sealed record ItemListItemResponse(
        int Id,
        string Name,
        string? CategorySlug,
        string? CategoryName,
        string? PrimaryType,
        string? SecondaryType,
        string? ObjectClass,
        string? WikiUrl,
        DateTime LastUpdated,
        ItemImageResponse? PrimaryImage);
}