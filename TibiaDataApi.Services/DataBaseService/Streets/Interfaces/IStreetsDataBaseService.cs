using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Streets;

namespace TibiaDataApi.Services.DataBaseService.Streets.Interfaces
{
    public interface IStreetsDataBaseService
    {
        Task<IReadOnlyList<StreetListItemResponse>> GetStreetsAsync(CancellationToken cancellationToken = default);
        Task<StreetDetailsResponse?> GetStreetDetailsByNameAsync(string streetName, CancellationToken cancellationToken = default);
        Task<StreetDetailsResponse?> GetStreetDetailsByIdAsync(int streetId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetStreetSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetStreetSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}