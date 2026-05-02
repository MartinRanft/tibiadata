namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyRevelationSlotResponse(
        int Id,
        string Vocation,
        string SlotKey,
        string Quarter,
        int RequiredPoints,
        WheelOfDestinyPerkReferenceResponse Perk,
        WheelOfDestinyPerkOccurrenceResponse? Occurrence);
}
