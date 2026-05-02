using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.WheelOfDestiny;
using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.DataBaseService.WheelOfDestiny.Interfaces
{
    public interface IWheelDataBaseService
    {
        Task<Dictionary<WheelVocation, List<string>>> GetPerkNamesAsync(CancellationToken cancellationToken = default);

        Task<PagedResponse<WheelOfDestinyPerkListItemResponse>> GetPerksAsync(
            int page,
            int pageSize,
            string? vocation = null,
            string? type = null,
            string? search = null,
            string? sort = null,
            bool descending = false,
            CancellationToken cancellationToken = default);

        Task<WheelOfDestinyPerkDetailsResponse?> GetPerkDetailsByIdAsync(int perkId, CancellationToken cancellationToken = default);
        
        Task<WheelOfDestinyPerkDetailsResponse?> GetPerkDetailsByKeyAsync(string perkKey, CancellationToken cancellationToken = default);
        
        Task<WheelOfDestinyPerkDetailsResponse?> GetPerkDetailsBySlugAsync(string slug, string vocation, CancellationToken cancellationToken = default);
        
        Task<WheelOfDestinyLayoutResponse?> GetLayoutByVocationAsync(string vocation, CancellationToken cancellationToken = default);

        Task<List<WheelOfDestinyGemResponse>> GetGemsAsync(string? vocation = null, CancellationToken cancellationToken = default);
        
        Task<PagedResponse<WheelOfDestinyGemModifierResponse>> GetGemModifiersAsync(
            int page,
            int pageSize,
            string? modifierType = null,
            string? category = null,
            string? vocation = null,
            string? search = null,
            bool? hasTradeoff = null,
            bool? isComboMod = null,
            string? sort = null,
            bool descending = false,
            CancellationToken cancellationToken = default);
        
        Task<List<SyncStateResponse>> GetPerkSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetPerkSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetGemSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetGemSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetGemModifierSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetGemModifierSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}