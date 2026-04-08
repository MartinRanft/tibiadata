namespace TibiaDataApi.Contracts.Public.WikiArticles
{
    public sealed record WikiArticleListItemResponse(
        int Id,
        string ContentType,
        string Title,
        string? Summary,
        string? InfoboxTemplate,
        string? WikiUrl,
        DateTime LastUpdated);
}