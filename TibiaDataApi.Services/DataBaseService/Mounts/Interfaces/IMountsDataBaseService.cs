using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Mounts;

namespace TibiaDataApi.Services.DataBaseService.Mounts.Interfaces
{
    public interface IMountsDataBaseService
    {
        Task<IReadOnlyList<MountListItemResponse>> GetMountsAsync(CancellationToken cancellationToken = default);
        Task<MountDetailsResponse?> GetMountDetailsByNameAsync(string mountName, CancellationToken cancellationToken = default);
        Task<MountDetailsResponse?> GetMountDetailsByIdAsync(int mountId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetMountSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetMountSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}