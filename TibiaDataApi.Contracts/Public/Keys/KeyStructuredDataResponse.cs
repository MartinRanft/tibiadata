namespace TibiaDataApi.Contracts.Public.Keys
{
    public sealed record KeyStructuredDataResponse(
        string? Template,
        KeyInfoboxResponse? Infobox);
}