namespace TibiaDataApi.Contracts.Public.Effects
{
    public sealed record EffectDetailsResponse(
        int Id,
        string Name,
        string? Summary,
        string? PlainTextContent,
        string? RawWikiText,
        EffectStructuredDataResponse? StructuredData,
        string? WikiUrl,
        DateTime? LastSeenAt,
        DateTime LastUpdated);
}