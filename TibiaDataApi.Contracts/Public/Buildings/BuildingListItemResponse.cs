namespace TibiaDataApi.Contracts.Public.Buildings
{
    public sealed record BuildingListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}