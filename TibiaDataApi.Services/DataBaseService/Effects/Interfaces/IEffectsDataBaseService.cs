using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Effects;

namespace TibiaDataApi.Services.DataBaseService.Effects.Interfaces
{
    public interface IEffectsDataBaseService
    {
        Task<IReadOnlyList<EffectListItemResponse>?> GetEffectNamesAsync(CancellationToken cancellationToken = default);
        Task<EffectDetailsResponse?> GetEffectDetailsByNameAsync(string effectName, CancellationToken cancellationToken = default);
        Task<EffectDetailsResponse?> GetEffectDetailsByIdAsync(int effectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetEffectSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetEffectSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}