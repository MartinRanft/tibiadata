using TibiaDataApi.Contracts.Public.Categories;

namespace TibiaDataApi.Services.DataBaseService.Categories.Interfaces
{
    public interface ICategoriesDataBaseService
    {
        Task<IReadOnlyList<CategoryListItemResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<CategoryDetailsResponse?> GetCategoryDetailsBySlugAsync(string slug, CancellationToken cancellationToken = default);
        Task<CategoryDetailsResponse?> GetCategoryDetailsByIdAsync(int categoryId, CancellationToken cancellationToken = default);
    }
}