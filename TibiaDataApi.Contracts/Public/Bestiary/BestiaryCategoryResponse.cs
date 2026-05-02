namespace TibiaDataApi.Contracts.Public.Bestiary
{
    public sealed record BestiaryCategoryResponse(
        int Id,
        string Name,
        string Slug,
        string ClassName,
        string ClassSlug,
        int CreatureCount);
}
