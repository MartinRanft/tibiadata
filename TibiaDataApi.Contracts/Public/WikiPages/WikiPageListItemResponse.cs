namespace TibiaDataApi.Contracts.Public.WikiPages
{
    public sealed record WikiPageListItemResponse(
        int Id,
        string Title,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}