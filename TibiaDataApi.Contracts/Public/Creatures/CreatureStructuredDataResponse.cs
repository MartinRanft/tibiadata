namespace TibiaDataApi.Contracts.Public.Creatures
{
    public sealed record CreatureStructuredDataResponse(
        string? Template,
        CreatureInfoboxResponse? Infobox);
}
