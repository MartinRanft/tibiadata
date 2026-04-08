namespace TibiaDataApi.Contracts.Public.Outfits
{
    public sealed record OutfitStructuredDataResponse(
        string? Template,
        OutfitInfoboxResponse? Infobox);
}