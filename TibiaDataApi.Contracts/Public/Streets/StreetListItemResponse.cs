namespace TibiaDataApi.Contracts.Public.Streets
{
    public sealed record StreetListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}