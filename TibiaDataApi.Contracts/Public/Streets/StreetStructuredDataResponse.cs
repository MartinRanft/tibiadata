namespace TibiaDataApi.Contracts.Public.Streets
{
    public sealed record StreetStructuredDataResponse(
        string? Template,
        StreetInfoboxResponse? Infobox);
}