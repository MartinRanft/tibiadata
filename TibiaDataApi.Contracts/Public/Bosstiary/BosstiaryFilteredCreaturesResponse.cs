namespace TibiaDataApi.Contracts.Public.Bosstiary
{
    public sealed record BosstiaryFilteredCreaturesResponse(
        string? CategorySlug,
        int? TotalPoints,
        string? Search,
        string? SortBy,
        bool Descending,
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<BosstiaryCreatureListItemResponse> Items);
}
