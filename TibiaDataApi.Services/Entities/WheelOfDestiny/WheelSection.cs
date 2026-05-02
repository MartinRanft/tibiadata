namespace TibiaDataApi.Services.Entities.WheelOfDestiny
{
    public sealed class WheelSection
    {
        public int Id { get; set; }

        public WheelVocation Vocation { get; set; }

        public required string SectionKey { get; set; }

        public WheelQuarter Quarter { get; set; }

        public byte RadiusIndex { get; set; }

        public short SortOrder { get; set; }

        public short SectionPoints { get; set; }

        public int ConvictionWheelPerkId { get; set; }

        public WheelPerk ConvictionWheelPerk { get; set; } = null!;

        public int? ConvictionWheelPerkOccurrenceId { get; set; }

        public WheelPerkOccurrence? ConvictionWheelPerkOccurrence { get; set; }

        public List<WheelSectionDedicationPerk> DedicationPerks { get; set; } = [];
    }
}
