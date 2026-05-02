using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Items;

namespace TibiaDataApi.Services.DataBaseService.Items.Interfaces
{
    public interface IItemsDataBaseService
    {
        Task<List<string>> GetItemNamesAsync(CancellationToken cancellationToken = default);

        Task<PagedResponse<ItemListItemResponse>> GetItemsAsync(
            int page,
            int pageSize,
            string? itemName = null,
            string? category = null,
            string? primaryType = null,
            string? secondaryType = null,
            string? objectClass = null,
            string? vocation = null,
            string? sort = null,
            bool descending = false,
            CancellationToken cancellationToken = default);

        Task<ItemDetailsResponse?> GetItemByNameAsync(
            string name,
            CancellationToken cancellationToken = default);

        Task<ItemDetailsResponse?> GetItemByIdAsync(
            int? id,
            CancellationToken cancellationToken = default);

        Task<List<string>> GetItemCategoriesAsync(
            CancellationToken cancellationToken = default);

        Task<List<ItemListItemResponse>> GetItemsByCategoryAsync(
            string categorySlug,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetItemUpdates(CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetItemUpdatesByDate(DateTime time, CancellationToken cancellationToken = default);
    }
}
