namespace TibiaDataApi.Contracts.Public.Missiles
{
    public sealed record MissileListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}