namespace TibiaDataApi.Contracts.Public.Quests
{
    public sealed record QuestInfoboxResponse(
        string? Name,
        string? Aka,
        string? Type,
        string? Implemented,
        string? Premium,
        string? Level,
        string? LevelRecommended,
        string? LevelNote,
        string? Location,
        string? Dangers,
        string? Legend,
        string? Reward,
        string? Log,
        string? Time,
        string? TimeAllocation,
        string? Transcripts,
        string? RookgaardQuest,
        string? History,
        string? Status,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? Fields);
}
