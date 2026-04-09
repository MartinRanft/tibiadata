namespace TibiaDataApi.Contracts.Public.Npcs
{
    public sealed record NpcDetailsResponse(
        int Id,
        string Name,
        string? Summary,
        string? PlainTextContent,
        string? RawWikiText,
        NpcStructuredDataResponse? StructuredData,
        string? WikiUrl,
        DateTime? LastSeenAt,
        DateTime LastUpdated);
}