namespace TibiaDataApi.Contracts.Public.LootStatistics
{
    public sealed record LootStatisticEntryResponse(
        string? ItemName,
        string? Chance,
        string? Rarity,
        string? Raw);
}