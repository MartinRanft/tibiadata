namespace TibiaDataApi.Contracts.Public.Bosstiary
{
    public sealed record BosstiaryCategoryCreaturesResponse(
        string CategoryName,
        string CategorySlug,
        int SortOrder,
        int TotalPoints,
        int TotalKillsRequired,
        IReadOnlyList<BosstiaryLevelRequirementResponse> LevelRequirements,
        int CreatureCount,
        IReadOnlyList<BosstiaryCreatureListItemResponse> Creatures);
}
