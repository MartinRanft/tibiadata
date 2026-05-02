namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyPerkOccurrenceResponse(
        int Id,
        int? Domain,
        int OccurrenceIndex,
        int? RequiredPoints,
        bool IsStackable,
        string? Notes);
}
