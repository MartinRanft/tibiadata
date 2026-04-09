namespace TibiaDataApi.Contracts.Public.Achievements
{
    public sealed record AchievementDetailsResponse(
        int Id,
        string Name,
        string? ActualName,
        string? Description,
        string? Spoiler,
        string? Grade,
        string? Points,
        string? Premium,
        string? Secret,
        string? Implemented,
        string? AchievementId,
        string? RelatedPages,
        string? History,
        string? Status,
        string? PlainTextContent,
        string? RawWikiText,
        AchievementStructuredDataResponse? StructuredData,
        string? WikiUrl,
        DateTime? LastSeenAt,
        DateTime LastUpdated);
}