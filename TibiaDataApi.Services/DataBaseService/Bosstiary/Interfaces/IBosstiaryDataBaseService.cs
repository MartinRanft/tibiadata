using TibiaDataApi.Contracts.Public.Bosstiary;

namespace TibiaDataApi.Services.DataBaseService.Bosstiary.Interfaces
{
    public interface IBosstiaryDataBaseService
    {
        Task<IReadOnlyList<BosstiaryCategoryResponse>> GetBosstiaryCategoriesAsync(CancellationToken cancellationToken = default);
        Task<BosstiaryCategoryResponse?> GetBosstiaryCategoryBySlugAsync(string categorySlug, CancellationToken cancellationToken = default);
        Task<BosstiaryCategoryResponse?> GetBosstiaryCategoryByIdAsync(int categoryId, CancellationToken cancellationToken = default);

        Task<BosstiaryCategoryCreaturesResponse?> GetBosstiaryCreaturesByCategoryAsync(
            string categorySlug,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BosstiaryPointOverviewItemResponse>> GetBosstiaryPointOverviewAsync(
            string? sortBy = null,
            bool descending = false,
            CancellationToken cancellationToken = default);

        Task<BosstiaryFilterOptionsResponse> GetBosstiaryFilterOptionsAsync(CancellationToken cancellationToken = default);

        Task<BosstiaryFilteredCreaturesResponse> GetFilteredBosstiaryCreaturesAsync(
            string? categorySlug = null,
            int? totalPoints = null,
            string? search = null,
            string? sortBy = null,
            bool descending = false,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);
    }
}
