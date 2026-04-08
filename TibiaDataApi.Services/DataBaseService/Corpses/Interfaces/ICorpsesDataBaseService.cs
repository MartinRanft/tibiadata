using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Corpses;

namespace TibiaDataApi.Services.DataBaseService.Corpses.Interfaces
{
    public interface ICorpsesDataBaseService
    {
        Task<IReadOnlyList<CorpseListItemResponse?>> GetCorpseNamesAsync(CancellationToken cancellationToken = default);
        Task<CorpseDetailsResponse?> GetCorpseDetailsByNameAsync(string corpseName, CancellationToken cancellationToken = default);
        Task<CorpseDetailsResponse?> GetCorpseDetailsByIdAsync(int corpseId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetCorpseSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetCorpseSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}