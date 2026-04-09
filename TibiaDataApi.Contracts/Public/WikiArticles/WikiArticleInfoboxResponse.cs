namespace TibiaDataApi.Contracts.Public.WikiArticles
{
    public sealed record WikiArticleInfoboxResponse(
        string? Name,
        string? ActualName,
        string? Implemented,
        string? Type,
        string? PrimaryType,
        string? SecondaryType,
        string? Article,
        string? Location,
        string? Notes,
        string? History,
        string? Status);
}