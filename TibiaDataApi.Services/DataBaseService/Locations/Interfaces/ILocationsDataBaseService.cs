using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Locations;

namespace TibiaDataApi.Services.DataBaseService.Locations.Interfaces
{
    public interface ILocationsDataBaseService
    {
        Task<IReadOnlyList<LocationListItemResponse>> GetLocationsAsync(CancellationToken cancellationToken = default);
        Task<LocationDetailsResponse?> GetLocationDetailsByNameAsync(string locationName, CancellationToken cancellationToken = default);
        Task<LocationDetailsResponse?> GetLocationDetailsByIdAsync(int locationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetLocationSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetLocationSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}