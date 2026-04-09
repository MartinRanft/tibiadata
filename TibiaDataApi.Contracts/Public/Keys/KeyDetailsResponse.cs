namespace TibiaDataApi.Contracts.Public.Keys
{
    public sealed record KeyDetailsResponse(
        int Id,
        string Name,
        string? Summary,
        string? PlainTextContent,
        string? RawWikiText,
        KeyStructuredDataResponse? StructuredData,
        string? WikiUrl,
        DateTime? LastSeenAt,
        DateTime LastUpdated);
}