namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryCharmPointOverviewItemResponse(
        int CreatureId,
        string CreatureName,
        string ClassName,
        string CategoryName,
        string Difficulty,
        int DifficultySortOrder,
        int CharmPoints,
        int TotalKillsRequired,
        DateTime LastUpdated);
}
