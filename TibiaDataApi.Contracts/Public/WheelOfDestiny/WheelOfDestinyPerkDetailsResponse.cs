namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyPerkDetailsResponse(
        int Id,
        string Key,
        string Slug,
        string Vocation,
        string Type,
        string Name,
        string? Summary,
        string? Description,
        string? MainSourceTitle,
        string? MainSourceUrl,
        bool IsGenericAcrossVocations,
        bool IsActive,
        string? MetadataJson,
        IReadOnlyList<WheelOfDestinyPerkOccurrenceResponse> Occurrences,
        IReadOnlyList<WheelOfDestinyPerkStageResponse> Stages,
        DateTime LastUpdated);
}
