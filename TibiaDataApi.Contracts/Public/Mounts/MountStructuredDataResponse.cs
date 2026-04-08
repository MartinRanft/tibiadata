namespace TibiaDataApi.Contracts.Public.Mounts
{
    public sealed record MountStructuredDataResponse(
        string? Template,
        MountInfoboxResponse? Infobox);
}