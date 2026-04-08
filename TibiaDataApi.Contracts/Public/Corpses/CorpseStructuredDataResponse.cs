namespace TibiaDataApi.Contracts.Public.Corpses
{
    public sealed record CorpseStructuredDataResponse(
        string? Template,
        CorpseInfoboxResponse? Infobox);
}