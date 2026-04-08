using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Spells;

namespace TibiaDataApi.Services.DataBaseService.Spells.Interfaces
{
    public interface ISpellsDataBaseService
    {
        Task<IReadOnlyList<SpellListItemResponse>> GetSpellsAsync(CancellationToken cancellationToken = default);
        Task<SpellDetailsResponse?> GetSpellDetailsByNameAsync(string spellName, CancellationToken cancellationToken = default);
        Task<SpellDetailsResponse?> GetSpellDetailsByIdAsync(int spellId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetSpellSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetSpellSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}