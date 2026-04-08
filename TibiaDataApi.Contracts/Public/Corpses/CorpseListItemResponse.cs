namespace TibiaDataApi.Contracts.Public.Corpses
{
    public sealed record CorpseListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}