namespace TibiaDataApi.Contracts.Public.Npcs
{
    public sealed record NpcStructuredDataResponse(
        string? Template,
        NpcInfoboxResponse? Infobox);
}