namespace TibiaDataApi.Contracts.Public.Quests
{
    public sealed record QuestRequirementResponse(
        string Key,
        string Label,
        string Value);
}
