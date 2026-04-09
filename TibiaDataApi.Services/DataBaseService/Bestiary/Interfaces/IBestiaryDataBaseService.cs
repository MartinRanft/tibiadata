using TibiaDataApi.Contracts.Public.Bestiary;

namespace TibiaDataApi.Services.DataBaseService.Bestiary.Interfaces
{
    public interface IBestiaryDataBaseService
    {
        Task<IReadOnlyList<BestiaryClassResponse>> GetBestiaryClassesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BestiaryCategoryResponse>> GetBestiaryCategoriesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BestiaryDifficultyResponse>> GetBestiaryDifficultiesAsync(CancellationToken cancellationToken = default);

        Task<BestiaryCategoryCreaturesResponse?> GetBestiaryCreaturesByCategoryAsync(
            string categorySlug,
            CancellationToken cancellationToken = default);

        Task<BestiaryDifficultyCreaturesResponse?> GetBestiaryCreaturesByDifficultyAsync(
            string difficultySlug,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BestiaryCharmPointOverviewItemResponse>> GetBestiaryCharmPointOverviewAsync(
            string? sortBy = null,
            bool descending = false,
            CancellationToken cancellationToken = default);

        Task<BestiaryFilteredCreaturesResponse> GetFilteredBestiaryCreaturesAsync(
            string? classSlug = null,
            string? categorySlug = null,
            string? difficultySlug = null,
            int? charmPoints = null,
            string? search = null,
            string? sortBy = null,
            bool descending = false,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);
    }
}
