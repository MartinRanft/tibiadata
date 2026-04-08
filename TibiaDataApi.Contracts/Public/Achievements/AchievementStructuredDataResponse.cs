namespace TibiaDataApi.Contracts.Public.Achievements
{
    public sealed record AchievementStructuredDataResponse(
        string? Template,
        AchievementInfoboxResponse? Infobox);
}