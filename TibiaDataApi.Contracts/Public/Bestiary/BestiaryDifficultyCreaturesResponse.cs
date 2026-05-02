namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryDifficultyCreaturesResponse(
        string Difficulty,
        string DifficultySlug,
        int SortOrder,
        int CharmPoints,
        int TotalKillsRequired,
        IReadOnlyList<BestiaryLevelRequirementResponse> LevelRequirements,
        int CreatureCount,
        IReadOnlyList<BestiaryCreatureListItemResponse> Creatures);
}
