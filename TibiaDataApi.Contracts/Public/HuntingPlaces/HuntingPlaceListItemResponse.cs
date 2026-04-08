namespace TibiaDataApi.Contracts.Public.HuntingPlaces
{
    public sealed record HuntingPlaceListItemResponse(
        int Id,
        string Title,
        string? Summary,
        string? City,
        string? Location,
        string? Vocation,
        string? WikiUrl,
        DateTime LastUpdated);
}