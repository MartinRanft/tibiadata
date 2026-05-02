namespace TibiaDataApi.Contracts.Public.Charms
{
    public sealed record CharmListItemResponse(
        int Id,
        string Name,
        string? ActualName,
        string Type,
        string Cost,
        string Effect,
        string? WikiUrl,
        DateTime LastUpdated);
}