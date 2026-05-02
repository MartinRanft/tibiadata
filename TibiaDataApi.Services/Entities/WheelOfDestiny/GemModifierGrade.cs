namespace TibiaDataApi.Services.Entities.WheelOfDestiny
{
        public class GemModifierGrade
    {
        public int Id { get; set; }

                public int GemModifierId { get; set; }

                public GemModifier GemModifier { get; set; } = null!;

                public required GemGrade Grade { get; set; }

                public required string ValueText { get; set; }

                public decimal? ValueNumeric { get; set; }

                public bool IsIncomplete { get; set; }

                public DateTime LastUpdated { get; set; }
    }
}
