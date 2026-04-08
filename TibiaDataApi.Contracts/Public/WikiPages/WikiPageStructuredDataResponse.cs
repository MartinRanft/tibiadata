namespace TibiaDataApi.Contracts.Public.WikiPages
{
    public sealed record WikiPageStructuredDataResponse(
        string? Template,
        WikiPageInfoboxResponse? Infobox);
}