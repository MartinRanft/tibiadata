namespace TibiaDataApi.Contracts.Public.Npcs
{
    public sealed record NpcListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}