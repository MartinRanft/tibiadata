namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryLevelRequirementResponse(
        int Level,
        string Name,
        int KillsRequired);
}
