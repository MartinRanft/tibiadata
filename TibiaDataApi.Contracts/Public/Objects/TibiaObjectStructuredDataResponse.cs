namespace TibiaDataApi.Contracts.Public.Objects
{
    public sealed record TibiaObjectStructuredDataResponse(
        string? Template,
        TibiaObjectInfoboxResponse? Infobox);
}