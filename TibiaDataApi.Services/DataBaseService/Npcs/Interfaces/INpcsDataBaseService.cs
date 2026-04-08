using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Npcs;

namespace TibiaDataApi.Services.DataBaseService.Npcs.Interfaces
{
    public interface INpcsDataBaseService
    {
        Task<IReadOnlyList<NpcListItemResponse>> GetNpcsAsync(CancellationToken cancellationToken = default);
        Task<NpcDetailsResponse?> GetNpcDetailsByNameAsync(string npcName, CancellationToken cancellationToken = default);
        Task<NpcDetailsResponse?> GetNpcDetailsByIdAsync(int npcId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetNpcSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetNpcSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}