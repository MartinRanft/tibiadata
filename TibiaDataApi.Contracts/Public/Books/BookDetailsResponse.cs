namespace TibiaDataApi.Contracts.Public.Books
{
    public sealed record BookDetailsResponse(
        int Id,
        string Name,
        string? Summary,
        string? PlainTextContent,
        string? RawWikiText,
        BookStructuredDataResponse? StructuredData,
        string? WikiUrl,
        DateTime? LastSeenAt,
        DateTime LastUpdated);
}