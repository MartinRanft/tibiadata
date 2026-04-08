namespace TibiaDataApi.Contracts.Public.Buildings
{
    public sealed record BuildingInfoboxResponse(
        string? Name,
        string? Type,
        string? Implemented,
        string? City,
        string? Location,
        string? Street,
        string? Street2,
        string? Street3,
        string? HouseId,
        string? Size,
        string? Beds,
        string? Rent,
        string? OpenWindows,
        string? Floors,
        string? Rooms,
        string? Furnishings,
        string? History,
        string? Notes,
        string? Ownable,
        string? PositionX,
        string? PositionY,
        string? PositionZ,
        string? Image);
}