namespace TibiaDataApi.Contracts.Public.LootStatistics
{
    public sealed record LootStatisticDetailsResponse(
        int CreatureId,
        string CreatureName,
        IReadOnlyList<LootStatisticEntryResponse> LootStatistics,
        DateTime LastUpdated);
}