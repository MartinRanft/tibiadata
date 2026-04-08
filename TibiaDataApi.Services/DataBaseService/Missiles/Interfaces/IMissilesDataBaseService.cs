using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Missiles;

namespace TibiaDataApi.Services.DataBaseService.Missiles.Interfaces
{
    public interface IMissilesDataBaseService
    {
        Task<IReadOnlyList<MissileListItemResponse>> GetMissilesAsync(CancellationToken cancellationToken = default);
        Task<MissileDetailsResponse?> GetMissileDetailsByNameAsync(string missileName, CancellationToken cancellationToken = default);
        Task<MissileDetailsResponse?> GetMissileDetailsByIdAsync(int missileId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetMissileSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetMissileSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}