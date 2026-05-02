namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryCreatureListItemResponse(
        int CreatureId,
        string CreatureName,
        string ClassName,
        string ClassSlug,
        string CategoryName,
        string CategorySlug,
        string Difficulty,
        string DifficultySlug,
        int DifficultySortOrder,
        int CharmPoints,
        int TotalKillsRequired,
        IReadOnlyList<BestiaryLevelRequirementResponse> LevelRequirements,
        DateTime LastUpdated);
}
