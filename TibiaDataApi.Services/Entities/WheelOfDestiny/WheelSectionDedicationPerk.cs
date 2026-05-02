namespace TibiaDataApi.Services.Entities.WheelOfDestiny
{
    public sealed class WheelSectionDedicationPerk
    {
        public int Id { get; set; }

        public int WheelSectionId { get; set; }

        public WheelSection WheelSection { get; set; } = null!;

        public int WheelPerkId { get; set; }

        public WheelPerk WheelPerk { get; set; } = null!;

        public short SortOrder { get; set; }
    }
}
