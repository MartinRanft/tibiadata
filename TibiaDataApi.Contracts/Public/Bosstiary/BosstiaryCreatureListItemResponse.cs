namespace TibiaDataApi.Contracts.Public.Bosstiary
{
    public sealed record BosstiaryCreatureListItemResponse(
        int CreatureId,
        string CreatureName,
        string CategoryName,
        string CategorySlug,
        int CategorySortOrder,
        int TotalPoints,
        int TotalKillsRequired,
        IReadOnlyList<BosstiaryLevelRequirementResponse> LevelRequirements,
        DateTime LastUpdated);
}
