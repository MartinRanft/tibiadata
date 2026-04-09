namespace TibiaDataApi.Contracts.Public.Bosstiary
{
    public sealed record BosstiaryCategoryResponse(
        int Id,
        string Name,
        string Slug,
        int SortOrder,
        int TotalPoints,
        int TotalKillsRequired,
        int CreatureCount);
}
