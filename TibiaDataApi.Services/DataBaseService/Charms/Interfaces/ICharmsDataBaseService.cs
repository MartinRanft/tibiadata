using TibiaDataApi.Contracts.Public.Charms;
using TibiaDataApi.Contracts.Public.Common;

namespace TibiaDataApi.Services.DataBaseService.Charms.Interfaces
{
    public interface ICharmsDataBaseService
    {
        Task<IReadOnlyList<CharmListItemResponse>> GetCharmsAsync(CancellationToken cancellationToken = default);
        Task<CharmDetailsResponse?> GetCharmDetailsByNameAsync(string charmName, CancellationToken cancellationToken = default);
        Task<CharmDetailsResponse?> GetCharmDetailsByIdAsync(int charmId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetCharmSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetCharmSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}