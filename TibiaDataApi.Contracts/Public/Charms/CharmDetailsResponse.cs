namespace TibiaDataApi.Contracts.Public.Charms
{
    public sealed record CharmDetailsResponse(
        int Id,
        string Name,
        string? ActualName,
        string Type,
        string Cost,
        string Effect,
        string Implemented,
        string? Notes,
        string? History,
        string? Status,
        string? PlainTextContent,
        string? RawWikiText,
        CharmStructuredDataResponse? StructuredData,
        string? WikiUrl,
        DateTime? LastSeenAt,
        DateTime LastUpdated);
}