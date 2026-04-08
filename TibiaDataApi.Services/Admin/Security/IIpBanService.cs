namespace TibiaDataApi.Services.Admin.Security
{
    public interface IIpBanService
    {
        Task<bool> IsBlockedAsync(
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<IpBanPage> GetBansAsync(
            bool includeExpired = false,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);

        Task<IpBanMutationResult> BanAsync(
            IpBanMutationRequest request,
            CancellationToken cancellationToken = default);

        Task<IpBanMutationResult> UnbanAsync(
            IpBanRemovalRequest request,
            CancellationToken cancellationToken = default);
    }
}