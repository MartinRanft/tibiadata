using TibiaDataApi.Contracts.Public.Meta;

namespace TibiaDataApi.Services.DataBaseService.Meta.Interfaces
{
    public interface IMetaDataBaseService
    {
        Task<ApiVersionResponse> GetApiVersionAsync(CancellationToken cancellationToken = default);

        Task<ApiSnapshotResponse> GetApiSnapshotAsync(CancellationToken cancellationToken = default);

        Task<ApiDeltaFeedResponse> GetApiDeltaFeedAsync(
            DateTime sinceUtc,
            IReadOnlyCollection<string>? resources = null,
            int limit = 250,
            CancellationToken cancellationToken = default);
    }
}
