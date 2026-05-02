using TibiaDataApi.Contracts.Public.LootStatistics;

namespace TibiaDataApi.Contracts.Public.Creatures
{
    public sealed record CreatureDetailsResponse(
        int Id,
        string Name,
        int Hitpoints,
        long Experience,
        CreatureStructuredDataResponse? StructuredData,
        IReadOnlyList<LootStatisticEntryResponse> LootStatistics,
        IReadOnlyList<CreatureImageResponse> Images,
        DateTime LastUpdated);
}
