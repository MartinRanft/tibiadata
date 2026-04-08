namespace TibiaDataApi.Contracts.Public.Common
{
    public sealed record PagedResponse<T>(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<T> Items);
}