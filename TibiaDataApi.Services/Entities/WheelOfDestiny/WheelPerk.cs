namespace TibiaDataApi.Services.Entities.WheelOfDestiny
{
    public sealed class WheelPerk
    {
        public int Id { get; set; }

        public required string Key { get; set; }

        public required string Slug { get; set; }

        public WheelVocation Vocation { get; set; }

        public WheelPerkType Type { get; set; }

        public required string Name { get; set; }

        public string? Summary { get; set; }

        public string? Description { get; set; }

        public string? MainSourceTitle { get; set; }

        public string? MainSourceUrl { get; set; }

        public bool IsGenericAcrossVocations { get; set; }

        public bool IsActive { get; set; } = true;

        public string? MetadataJson { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public List<WheelPerkOccurrence> Occurrences { get; set; } = [];

        public List<WheelPerkStage> Stages { get; set; } = [];
    }
}
