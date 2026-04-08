namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryFilterOptionsResponse(
        IReadOnlyList<BestiaryClassResponse> Classes,
        IReadOnlyList<BestiaryCategoryResponse> Categories,
        IReadOnlyList<BestiaryDifficultyResponse> Difficulties);
}
