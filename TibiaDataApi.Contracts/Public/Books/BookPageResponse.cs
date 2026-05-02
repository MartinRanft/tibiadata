namespace TibiaDataApi.Contracts.Public.Books
{
    public sealed record BookPageResponse(
        int Index,
        string Text,
        string? ReturnPage,
        string? BookType,
        string? Location);
}
