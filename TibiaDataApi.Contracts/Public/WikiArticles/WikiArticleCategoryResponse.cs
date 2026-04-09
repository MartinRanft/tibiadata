namespace TibiaDataApi.Contracts.Public.WikiArticles
{
    public sealed record WikiArticleCategoryResponse(
        int CategoryId,
        string Slug,
        string Name,
        string GroupSlug,
        string GroupName);
}