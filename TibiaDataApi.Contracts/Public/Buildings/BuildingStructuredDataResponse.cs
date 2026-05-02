namespace TibiaDataApi.Contracts.Public.Buildings
{
    public sealed record BuildingStructuredDataResponse(
        string? Template,
        BuildingInfoboxResponse? Infobox,
        IReadOnlyList<BuildingAddressResponse> Addresses,
        BuildingCoordinatesResponse? Coordinates);
}
