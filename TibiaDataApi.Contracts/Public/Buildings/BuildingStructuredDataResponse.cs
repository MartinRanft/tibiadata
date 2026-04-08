namespace TibiaDataApi.Contracts.Public.Buildings
{
    public sealed record BuildingStructuredDataResponse(
        string? Template,
        BuildingInfoboxResponse? Infobox);
}