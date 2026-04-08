using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

using TibiaDataApi.Services.Entities.Security;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Admin.Security
{
    public sealed class AdminCredentialService(TibiaDbContext dbContext) : IAdminCredentialService
    {
        private const string PrimaryCredentialKey = "primary";
        private const string HashPrefix = "pbkdf2-sha256";
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public async Task<bool> HasConfiguredPasswordAsync(CancellationToken cancellationToken = default)
        {
            return await dbContext.AdminCredentials
                                  .AsNoTracking()
                                  .AnyAsync(
                                      entry => entry.Key == PrimaryCredentialKey &&
                                               !string.IsNullOrWhiteSpace(entry.PasswordHash),
                                      cancellationToken);
        }

        public async Task<bool> VerifyPasswordAsync(string providedPassword, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(providedPassword))
            {
                return false;
            }

            string? storedHash = await dbContext.AdminCredentials
                                                .AsNoTracking()
                                                .Where(entry => entry.Key == PrimaryCredentialKey)
                                                .Select(entry => entry.PasswordHash)
                                                .SingleOrDefaultAsync(cancellationToken);

            return VerifyPasswordHash(providedPassword, storedHash);
        }

        public async Task<bool> TryInitializePasswordAsync(string password, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            if(await HasConfiguredPasswordAsync(cancellationToken))
            {
                return false;
            }

            AdminCredential credential = new()
            {
                Key = PrimaryCredentialKey,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.AdminCredentials.Add(credential);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                dbContext.Entry(credential).State = EntityState.Detached;
                return false;
            }
        }

        public async Task SetPasswordAsync(string password, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password must not be empty.", nameof(password));
            }

            AdminCredential? existingCredential = await dbContext.AdminCredentials
                                                                 .SingleOrDefaultAsync(
                                                                     entry => entry.Key == PrimaryCredentialKey,
                                                                     cancellationToken);

            if(existingCredential is null)
            {
                dbContext.AdminCredentials.Add(new AdminCredential
                {
                    Key = PrimaryCredentialKey,
                    PasswordHash = HashPassword(password),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingCredential.PasswordHash = HashPassword(password);
                existingCredential.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        internal static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return string.Join(
                '$',
                HashPrefix,
                Iterations.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }

        internal static bool VerifyPasswordHash(string providedPassword, string? storedHash)
        {
            if(string.IsNullOrWhiteSpace(providedPassword) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            string[] parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if(parts.Length != 4 ||
               !string.Equals(parts[0], HashPrefix, StringComparison.Ordinal) ||
               !int.TryParse(parts[1], out int iterations) ||
               iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expectedHash;

            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedHash = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(providedPassword),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}