namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinySectionResponse(
        int Id,
        string Vocation,
        string SectionKey,
        string Quarter,
        int RadiusIndex,
        int SortOrder,
        int SectionPoints,
        WheelOfDestinyPerkReferenceResponse ConvictionPerk,
        WheelOfDestinyPerkOccurrenceResponse? ConvictionOccurrence,
        IReadOnlyList<WheelOfDestinySectionDedicationPerkResponse> DedicationPerks);
}
