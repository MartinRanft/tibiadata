namespace TibiaDataApi.Contracts.Public.LootStatistics
{
    public sealed record LootStatisticListItemResponse(
        int CreatureId,
        string CreatureName,
        DateTime LastUpdated);
}