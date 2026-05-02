namespace TibiaDataApi.Services.Entities.WheelOfDestiny
{
    public sealed class WheelPerkOccurrence
    {
        public int Id { get; set; }

        public int WheelPerkId { get; set; }

        public WheelPerk WheelPerk { get; set; } = null!;

        public byte? Domain { get; set; }

        public short OccurrenceIndex { get; set; } = 1;

        public short? RequiredPoints { get; set; }

        public bool IsStackable { get; set; }

        public string? Notes { get; set; }
    }
}
