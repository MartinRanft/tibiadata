namespace TibiaDataApi.Contracts.Public.HuntingPlaces
{
    public sealed record HuntingPlaceAreaRecommendationResponse(
        string? AreaName,
        string? LevelKnights,
        string? LevelPaladins,
        string? LevelMages,
        string? SkillKnights,
        string? SkillPaladins,
        string? SkillMages,
        string? DefenseKnights,
        string? DefensePaladins,
        string? DefenseMages);
}