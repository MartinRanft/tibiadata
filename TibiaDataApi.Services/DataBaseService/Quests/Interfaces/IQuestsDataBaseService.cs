using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Quests;

namespace TibiaDataApi.Services.DataBaseService.Quests.Interfaces
{
    public interface IQuestsDataBaseService
    {
        Task<IReadOnlyList<QuestListItemResponse>> GetQuestsAsync(CancellationToken cancellationToken = default);
        Task<QuestDetailsResponse?> GetQuestDetailsByNameAsync(string questName, CancellationToken cancellationToken = default);
        Task<QuestDetailsResponse?> GetQuestDetailsByIdAsync(int questId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetQuestSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetQuestSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}