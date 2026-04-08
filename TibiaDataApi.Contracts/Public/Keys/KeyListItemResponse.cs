namespace TibiaDataApi.Contracts.Public.Keys
{
    public sealed record KeyListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}