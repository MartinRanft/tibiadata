namespace TibiaDataApi.Contracts.Public.Search
{
    public sealed record SearchResponse(
        string Query,
        int TotalCount,
        IReadOnlyList<SearchResultItemResponse> Items);
}