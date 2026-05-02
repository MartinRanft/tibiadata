using TibiaDataApi.Contracts.Public.Search;

namespace TibiaDataApi.Services.DataBaseService.Search.Interfaces
{
    public interface ISearchDataBaseService
    {
        IReadOnlyList<string> GetSupportedTypes();

        Task<SearchResponse> SearchAsync(
            string query,
            IReadOnlyList<string>? types = null,
            int limit = 20,
            CancellationToken cancellationToken = default);
    }
}
