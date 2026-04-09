namespace TibiaDataApi.Contracts.Public.WikiPages
{
    public sealed record WikiPageDetailsResponse(
        int Id,
        string Title,
        string? Summary,
        string? PlainTextContent,
        string? RawWikiText,
        WikiPageStructuredDataResponse? StructuredData,
        string? WikiUrl,
        DateTime? LastSeenAt,
        DateTime LastUpdated);
}