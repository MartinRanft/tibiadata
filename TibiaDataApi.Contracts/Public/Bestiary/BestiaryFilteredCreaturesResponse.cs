namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryFilteredCreaturesResponse(
        string? BestiaryClass,
        string? Category,
        string? Difficulty,
        int? CharmPointReward,
        string? CreatureName,
        string? Sort,
        bool Descending,
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<BestiaryCreatureListItemResponse> Creatures);
}
