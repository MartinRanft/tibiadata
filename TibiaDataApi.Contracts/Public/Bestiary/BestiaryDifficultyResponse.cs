namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryDifficultyResponse(
        string Name,
        string Slug,
        int SortOrder,
        int CharmPoints,
        int TotalKillsRequired,
        IReadOnlyList<BestiaryLevelRequirementResponse> LevelRequirements);
}
