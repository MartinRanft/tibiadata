namespace TibiaDataApi.Contracts.Public.Spells
{
    public sealed record SpellStructuredDataResponse(
        string? Template,
        SpellInfoboxResponse? Infobox);
}