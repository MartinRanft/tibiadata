namespace TibiaDataApi.Services.Admin.Security
{
    public interface IAdminCredentialService
    {
        Task<bool> HasConfiguredPasswordAsync(CancellationToken cancellationToken = default);

        Task<bool> VerifyPasswordAsync(string providedPassword, CancellationToken cancellationToken = default);

        Task<bool> TryInitializePasswordAsync(string password, CancellationToken cancellationToken = default);

        Task SetPasswordAsync(string password, CancellationToken cancellationToken = default);
    }
}