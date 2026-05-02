namespace TibiaDataApi.Contracts.Public.Achievements
{
    public sealed record AchievementListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}