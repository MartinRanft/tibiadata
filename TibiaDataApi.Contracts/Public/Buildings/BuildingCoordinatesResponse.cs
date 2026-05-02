namespace TibiaDataApi.Contracts.Public.Buildings
{
    public sealed record BuildingCoordinatesResponse(
        decimal? X,
        decimal? Y,
        int? Z);
}
