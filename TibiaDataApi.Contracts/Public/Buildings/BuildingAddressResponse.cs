namespace TibiaDataApi.Contracts.Public.Buildings
{
    public sealed record BuildingAddressResponse(
        int Index,
        string Street,
        string? City,
        string? Location);
}
