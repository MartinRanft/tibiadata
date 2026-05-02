namespace TibiaDataApi.Contracts.Public.Quests
{
    public sealed record QuestStructuredDataResponse(
        string? Template,
        QuestInfoboxResponse? Infobox,
        IReadOnlyList<QuestRequirementResponse> Requirements,
        IReadOnlyList<QuestRewardResponse> Rewards);
}
