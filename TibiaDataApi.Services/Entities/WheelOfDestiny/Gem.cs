namespace TibiaDataApi.Services.Entities.WheelOfDestiny
{
        public class Gem
    {
        public int Id { get; set; }

                public required string Name { get; set; }

                public required string WikiUrl { get; set; }

                public required GemFamily GemFamily { get; set; }

                public required GemSize GemSize { get; set; }

                public GemVocation? VocationRestriction { get; set; }

                public string? Description { get; set; }

                public DateTime LastUpdated { get; set; }
    }
}
