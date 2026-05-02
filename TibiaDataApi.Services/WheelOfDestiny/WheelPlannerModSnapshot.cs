using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.WheelOfDestiny
{
        public sealed record WheelPlannerModSnapshot(
        string VariantKey,
        string Name,
        GemModifierType Type,
        GemModifierCategory Category,
        GemVocation? VocationRestriction,
        Dictionary<GemGrade, string> GradeValues,
        string? Description);
}
