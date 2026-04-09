using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Creatures;

namespace TibiaDataApi.Services.DataBaseService.Creatures.Interfaces
{
    public interface ICreaturesDataBaseService
    {
        Task<List<string>> GetCreaturesAsync(CancellationToken cancellationToken = default);

        Task<CreatureDetailsResponse?> GetCreatureDetailsByNameAsync(
            string creatureName,
            CancellationToken cancellationToken = default);

        Task<CreatureDetailsResponse?> GetCreatureDetailsByIdAsync(
            int creatureId,
            CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetCreatureSyncStatesAsync(CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetCreatureSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}