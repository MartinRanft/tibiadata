namespace TibiaDataApi.Contracts.Public.Locations
{
    public sealed record LocationStructuredDataResponse(
        string? Template,
        LocationInfoboxResponse? Infobox);
}