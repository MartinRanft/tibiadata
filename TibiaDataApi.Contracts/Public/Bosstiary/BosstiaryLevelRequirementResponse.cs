namespace TibiaDataApi.Contracts.Public.Bosstiary
{
    public sealed record BosstiaryLevelRequirementResponse(
        int Level,
        string Name,
        int KillsRequired,
        int PointsAwarded);
}
