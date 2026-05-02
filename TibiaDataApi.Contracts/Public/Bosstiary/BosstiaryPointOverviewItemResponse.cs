namespace TibiaDataApi.Contracts.Public.Bosstiary
{
    public sealed record BosstiaryPointOverviewItemResponse(
        int CreatureId,
        string CreatureName,
        string CategoryName,
        int CategorySortOrder,
        int TotalPoints,
        int TotalKillsRequired,
        DateTime LastUpdated);
}
