namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyGemModifierResponse(
        int Id,
        string Name,
        string VariantKey,
        string WikiUrl,
        string ModifierType,
        string Category,
        string? VocationRestriction,
        bool IsComboMod,
        bool HasTradeoff,
        string? Description,
        IReadOnlyList<WheelOfDestinyGemModifierGradeResponse> Grades,
        DateTime LastUpdated);
}
