namespace TibiaDataApi.Contracts.Public.Charms
{
    public sealed record CharmStructuredDataResponse(
        string? Template,
        CharmInfoboxResponse? Infobox);
}