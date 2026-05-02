namespace TibiaDataApi.Services.Entities.WheelOfDestiny
{
        public class GemModifier
    {
        public int Id { get; set; }

                public required string Name { get; set; }

                public required string VariantKey { get; set; }

                public required string WikiUrl { get; set; }

                public required GemModifierType ModifierType { get; set; }

                public required GemModifierCategory Category { get; set; }

                public GemVocation? VocationRestriction { get; set; }

                public bool IsComboMod { get; set; }

                public bool HasTradeoff { get; set; }

                public string? Description { get; set; }

                public DateTime LastUpdated { get; set; }

                public ICollection<GemModifierGrade> Grades { get; set; } = new List<GemModifierGrade>();
    }
}
