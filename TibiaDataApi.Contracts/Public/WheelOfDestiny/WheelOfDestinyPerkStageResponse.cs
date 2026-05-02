namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyPerkStageResponse(
        int Id,
        int Stage,
        string UnlockKind,
        int UnlockValue,
        string? EffectSummary,
        string? EffectDetailsJson,
        int SortOrder);
}
