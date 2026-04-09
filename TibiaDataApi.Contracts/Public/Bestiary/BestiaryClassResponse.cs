namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryClassResponse(
        int Id,
        string Name,
        string Slug,
        int SortOrder,
        int CategoryCount,
        int CreatureCount);
}
