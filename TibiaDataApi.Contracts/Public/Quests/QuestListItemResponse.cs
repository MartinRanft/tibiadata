namespace TibiaDataApi.Contracts.Public.Quests
{
    public sealed record QuestListItemResponse(
        int Id,
        string Name,
        string? Summary,
        string? WikiUrl,
        DateTime LastUpdated);
}