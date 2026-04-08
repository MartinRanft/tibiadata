namespace TibiaDataApi.Contracts.Public.Missiles
{
    public sealed record MissileStructuredDataResponse(
        string? Template,
        MissileInfoboxResponse? Infobox);
}