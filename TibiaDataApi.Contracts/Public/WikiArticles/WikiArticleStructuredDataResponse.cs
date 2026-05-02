namespace TibiaDataApi.Contracts.Public.WikiArticles
{
    public sealed record WikiArticleStructuredDataResponse(
        string? Template,
        WikiArticleInfoboxResponse? Infobox);
}