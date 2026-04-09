namespace TibiaDataApi.Contracts.Public.Locations
{
    public sealed record LocationDetailsResponse(
        int Id,
        string Name,
        string? Summary,
        string? PlainTextContent,
        string? RawWikiText,
        LocationStructuredDataResponse? StructuredData,
        string? WikiUrl,
        DateTime? LastSeenAt,
        DateTime LastUpdated);
}