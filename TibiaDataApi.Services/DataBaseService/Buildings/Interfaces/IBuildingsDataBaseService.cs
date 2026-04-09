using TibiaDataApi.Contracts.Public.Buildings;
using TibiaDataApi.Contracts.Public.Common;

namespace TibiaDataApi.Services.DataBaseService.Buildings.Interfaces
{
    public interface IBuildingsDataBaseService
    {
        Task<IReadOnlyList<BuildingListItemResponse>> GetBuildingsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BuildingListItemResponse>?> GetBuildingsByCityAsync(string city, CancellationToken cancellationToken = default);
        Task<BuildingDetailsResponse?> GetBuildingDetailsByNameAsync(string buildingName, CancellationToken cancellationToken = default);
        Task<BuildingDetailsResponse?> GetBuildingDetailsByIdAsync(int buildingId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetBuildingSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetBuildingSyncStatesSinceAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}