namespace TibiaDataApi.Services.Entities.WheelOfDestiny
{
    public sealed class WheelRevelationSlot
    {
        public int Id { get; set; }

        public WheelVocation Vocation { get; set; }

        public required string SlotKey { get; set; }

        public WheelQuarter Quarter { get; set; }

        public short RequiredPoints { get; set; }

        public int WheelPerkId { get; set; }

        public WheelPerk WheelPerk { get; set; } = null!;

        public int? WheelPerkOccurrenceId { get; set; }

        public WheelPerkOccurrence? WheelPerkOccurrence { get; set; }
    }
}
