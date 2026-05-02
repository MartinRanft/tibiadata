namespace TibiaDataApi.Contracts.Public.Mounts
{
    public sealed record MountListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}