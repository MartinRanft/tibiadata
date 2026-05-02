namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyGemResponse(
        int Id,
        string Name,
        string WikiUrl,
        string GemFamily,
        string GemSize,
        string? VocationRestriction,
        string? Description,
        DateTime LastUpdated);
}
