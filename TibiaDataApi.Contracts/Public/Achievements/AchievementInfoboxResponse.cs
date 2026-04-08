namespace TibiaDataApi.Contracts.Public.Achievements
{
    public sealed record AchievementInfoboxResponse(
        string? Name,
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
        string? CoincidesWith);
}