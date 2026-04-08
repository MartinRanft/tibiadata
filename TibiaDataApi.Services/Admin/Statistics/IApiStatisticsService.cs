namespace TibiaDataApi.Services.Admin.Statistics
{
    public interface IApiStatisticsService
    {
        Task RecordRequestAsync(
            ApiRequestRecord request,
            CancellationToken cancellationToken = default);

        Task<ApiStatisticsSummary> GetSummaryAsync(
            int days = 30,
            CancellationToken cancellationToken = default);

        Task<ApiRequestLogPage> GetRequestLogsAsync(
            int days = 1,
            string? ipAddress = null,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);

        Task<ApiIpActivityDetails> GetIpActivityAsync(
            string ipAddress,
            int days = 1,
            int recentRequestCount = 50,
            CancellationToken cancellationToken = default);
    }
}