namespace TibiaDataApi.Contracts.Public.Books
{
    public sealed record BookInfoboxResponse(
        string? BookType,
        string? BookType2,
        string? Title,
        string? PageName,
        string? Location,
        string? Blurb,
        string? Author,
        string? ReturnPage,
        string? ReturnPage2,
        string? PreviousBook,
        string? NextBook,
        string? RelatedPages,
        string? Text,
        string? Implemented,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? Fields);
}
