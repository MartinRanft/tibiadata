using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Objects;

namespace TibiaDataApi.Services.DataBaseService.Objects.Interfaces
{
    public interface IObjectsDataBaseService
    {
        Task<IReadOnlyList<TibiaObjectListItemResponse>> GetObjectsAsync(CancellationToken cancellationToken = default);
        Task<TibiaObjectDetailsResponse?> GetObjectDetailsByNameAsync(string objectName, CancellationToken cancellationToken = default);
        Task<TibiaObjectDetailsResponse?> GetObjectDetailsByIdAsync(int objectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetObjectSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetObjectSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}