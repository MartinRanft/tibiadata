namespace TibiaDataApi.Contracts.Public.Effects
{
    public sealed record EffectStructuredDataResponse(
        string? Template,
        EffectInfoboxResponse? Infobox);
}