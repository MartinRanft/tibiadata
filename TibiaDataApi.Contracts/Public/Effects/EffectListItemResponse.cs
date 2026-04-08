namespace TibiaDataApi.Contracts.Public.Effects
{
    public sealed record EffectListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}