namespace TibiaDataApi.Contracts.Public.WikiArticles
{
    public sealed record WikiArticleDetailsResponse(
        int Id,
        string ContentType,
        string Title,
        string? Summary,
        string? PlainTextContent,
        string? RawWikiText,
        WikiArticleStructuredDataResponse? StructuredData,
        IReadOnlyList<string> Sections,
        IReadOnlyList<string> LinkedTitles,
        IReadOnlyList<WikiArticleCategoryResponse> Categories,
        string? WikiUrl,
        DateTime? LastSeenAt,
        DateTime LastUpdated);
}