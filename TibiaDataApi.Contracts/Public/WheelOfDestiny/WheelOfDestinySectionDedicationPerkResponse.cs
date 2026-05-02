namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinySectionDedicationPerkResponse(
        int Id,
        int SortOrder,
        WheelOfDestinyPerkReferenceResponse Perk);
}
