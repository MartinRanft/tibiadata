namespace TibiaDataApi.Contracts.Public.Search
{
    public sealed record SearchResultItemResponse(
        string Kind,
        int Id,
        string Title,
        string? Subtitle,
        string? CategorySlug,
        string? CategoryName,
        string? Summary,
        string? Route,
        string? WikiUrl,
        DateTime LastUpdated);
}