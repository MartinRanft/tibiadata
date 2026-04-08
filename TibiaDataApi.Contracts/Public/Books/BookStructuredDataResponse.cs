namespace TibiaDataApi.Contracts.Public.Books
{
    public sealed record BookStructuredDataResponse(
        string? Template,
        BookInfoboxResponse? Infobox);
}