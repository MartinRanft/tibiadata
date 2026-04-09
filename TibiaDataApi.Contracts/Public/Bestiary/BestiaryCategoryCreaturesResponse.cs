namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryCategoryCreaturesResponse(
        string CategoryName,
        string CategorySlug,
        string ClassName,
        string ClassSlug,
        int CreatureCount,
        IReadOnlyList<BestiaryCreatureListItemResponse> Creatures);
}
