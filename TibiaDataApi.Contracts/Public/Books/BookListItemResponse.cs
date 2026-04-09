namespace TibiaDataApi.Contracts.Public.Books
{
    public sealed record BookListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}