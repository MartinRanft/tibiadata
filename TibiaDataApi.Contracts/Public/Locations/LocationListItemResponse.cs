namespace TibiaDataApi.Contracts.Public.Locations
{
    public sealed record LocationListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}