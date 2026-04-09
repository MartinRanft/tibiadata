namespace TibiaDataApi.Contracts.Public.Outfits
{
    public sealed record OutfitInfoboxResponse(
        string? Name,
        string? Outfit,
        string? PrimaryType,
        string? SecondaryType,
        string? MaleId,
        string? FemaleId,
        string? Implemented,
        string? Addons,
        string? Premium,
        string? Artwork,
        string? Bought,
        string? Achievement,
        string? BaseOutfitPrice,
        string? FullOutfitPrice,
        string? Addon1Price,
        string? Addon2Price,
        string? Notes,
        string? History,
        string? Status,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? Fields);
}
