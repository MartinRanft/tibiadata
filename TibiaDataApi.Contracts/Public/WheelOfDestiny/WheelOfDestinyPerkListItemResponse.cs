namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyPerkListItemResponse(
        int Id,
        string Key,
        string Slug,
        string Vocation,
        string Type,
        string Name,
        string? Summary,
        bool IsGenericAcrossVocations,
        bool IsActive,
        string? MainSourceTitle,
        string? MainSourceUrl,
        DateTime LastUpdated);
}
