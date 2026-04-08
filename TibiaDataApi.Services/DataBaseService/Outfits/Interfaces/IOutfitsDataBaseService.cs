using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Outfits;

namespace TibiaDataApi.Services.DataBaseService.Outfits.Interfaces
{
    public interface IOutfitsDataBaseService
    {
        Task<IReadOnlyList<OutfitListItemResponse>> GetOutfitsAsync(CancellationToken cancellationToken = default);
        Task<OutfitDetailsResponse?> GetOutfitDetailsByNameAsync(string outfitName, CancellationToken cancellationToken = default);
        Task<OutfitDetailsResponse?> GetOutfitDetailsByIdAsync(int outfitId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetOutfitSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SyncStateResponse>?> GetOutfitSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}