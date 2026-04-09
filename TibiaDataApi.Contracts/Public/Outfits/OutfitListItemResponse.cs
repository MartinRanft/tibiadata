namespace TibiaDataApi.Contracts.Public.Outfits
{
    public sealed record OutfitListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}