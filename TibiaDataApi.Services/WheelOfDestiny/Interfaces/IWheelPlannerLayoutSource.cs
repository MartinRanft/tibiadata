using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.WheelOfDestiny.Interfaces
{
    public interface IWheelPlannerLayoutSource
    {
                Task<WheelPlannerLayoutSnapshot> LoadAsync(CancellationToken cancellationToken = default);

                Task<WheelPlannerFullSnapshot> LoadFullAsync(CancellationToken cancellationToken = default);
    }

    public sealed record WheelPlannerLayoutSnapshot(
        IReadOnlyList<WheelPlannerSectionSnapshot> Sections,
        IReadOnlyList<WheelPlannerRevelationSlotSnapshot> RevelationSlots)
    {
        public static WheelPlannerLayoutSnapshot Empty { get; } = new([], []);
    }

    public sealed record WheelPlannerSectionSnapshot(
        WheelVocation Vocation,
        string SectionKey,
        WheelQuarter Quarter,
        byte RadiusIndex,
        short SortOrder,
        short SectionPoints,
        string DedicationText,
        string ConvictionText);

    public sealed record WheelPlannerRevelationSlotSnapshot(
        WheelVocation Vocation,
        string SlotKey,
        WheelQuarter Quarter,
        short RequiredPoints,
        string PerkName);
}
