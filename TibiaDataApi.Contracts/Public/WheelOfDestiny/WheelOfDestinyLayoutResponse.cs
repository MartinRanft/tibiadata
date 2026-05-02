namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyLayoutResponse(
        string Vocation,
        IReadOnlyList<WheelOfDestinySectionResponse> Sections,
        IReadOnlyList<WheelOfDestinyRevelationSlotResponse> RevelationSlots);
}
