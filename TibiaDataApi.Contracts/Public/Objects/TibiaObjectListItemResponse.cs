namespace TibiaDataApi.Contracts.Public.Objects
{
    public sealed record TibiaObjectListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}