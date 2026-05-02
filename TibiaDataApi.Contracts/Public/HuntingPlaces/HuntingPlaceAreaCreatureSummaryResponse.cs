namespace TibiaDataApi.Contracts.Public.HuntingPlaces
{
    public sealed record HuntingPlaceAreaCreatureSummaryResponse(
        string AreaName,
        string? SectionName,
        int CreatureCount,
        IReadOnlyList<HuntingPlaceCreatureResponse> Creatures,
        HuntingPlaceVocationValueResponse? RecommendedLevels,
        HuntingPlaceVocationValueResponse? RecommendedSkills,
        HuntingPlaceVocationValueResponse? RecommendedDefense);
}
