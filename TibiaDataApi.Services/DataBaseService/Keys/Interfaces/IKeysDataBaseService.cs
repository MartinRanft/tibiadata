using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Keys;

namespace TibiaDataApi.Services.DataBaseService.Keys.Interfaces
{
    public interface IKeysDataBaseService
    {
        Task<IReadOnlyList<KeyListItemResponse>> GetKeysAsync(CancellationToken cancellationToken = default);
        Task<KeyDetailsResponse?> GetKeyDetailsByNameAsync(string keyName, CancellationToken cancellationToken = default);
        Task<KeyDetailsResponse?> GetKeyDetailsByIdAsync(int keyId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetKeySyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetKeySyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}