namespace TibiaDataApi.Contracts.Public.Categories
{
    public sealed record CategoryListItemResponse(
        int Id,
        string Slug,
        string Name,
        string ContentType,
        string GroupSlug,
        string GroupName,
        string? ObjectClass,
        int SortOrder);
}