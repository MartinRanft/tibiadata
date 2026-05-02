using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Creatures;
using TibiaDataApi.Contracts.Public.LootStatistics;

namespace TibiaDataApi.Services.DataBaseService.Creatures.Interfaces
{
    public interface ICreaturesDataBaseService
    {
        Task<List<string>> GetCreaturesAsync(CancellationToken cancellationToken = default);

        Task<PagedResponse<CreatureListItemResponse>> GetCreatureListAsync(
            int page,
            int pageSize,
            string? creatureName = null,
            int? minHitpoints = null,
            int? maxHitpoints = null,
            long? minExperience = null,
            long? maxExperience = null,
            string? sort = null,
            bool descending = false,
            CancellationToken cancellationToken = default);

        Task<CreatureDetailsResponse?> GetCreatureDetailsByNameAsync(
            string creatureName,
            CancellationToken cancellationToken = default);

        Task<CreatureDetailsResponse?> GetCreatureDetailsByIdAsync(
            int creatureId,
            CancellationToken cancellationToken = default);

        Task<LootStatisticDetailsResponse?> GetCreatureLootByNameAsync(
            string creatureName,
            CancellationToken cancellationToken = default);

        Task<LootStatisticDetailsResponse?> GetCreatureLootByIdAsync(
            int creatureId,
            CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetCreatureSyncStatesAsync(CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetCreatureSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}
