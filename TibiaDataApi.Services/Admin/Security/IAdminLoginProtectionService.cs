namespace TibiaDataApi.Services.Admin.Security
{
    public interface IAdminLoginProtectionService
    {
        Task<AdminLoginProtectionResult> RegisterFailedAttemptAsync(
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task ResetFailuresAsync(
            string? ipAddress,
            CancellationToken cancellationToken = default);
    }
}