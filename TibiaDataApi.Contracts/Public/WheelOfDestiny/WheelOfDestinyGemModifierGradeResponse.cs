namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyGemModifierGradeResponse(
        int Id,
        string Grade,
        string ValueText,
        decimal? ValueNumeric,
        bool IsIncomplete,
        DateTime LastUpdated);
}
