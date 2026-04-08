namespace TibiaDataApi.Contracts.Public.Spells
{
    public sealed record SpellListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}