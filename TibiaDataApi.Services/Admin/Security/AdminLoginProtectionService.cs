using System.Net;

using Microsoft.EntityFrameworkCore;

using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Entities.Security;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Admin.Security
{
    public sealed class AdminLoginProtectionService(
        TibiaDbContext dbContext,
        ICacheInvalidationService cacheInvalidationService) : IAdminLoginProtectionService
    {
        public const int FailureThreshold = 5;
        private const string AutomaticBanReason = "Automatic admin login lockout after 5 failed password attempts.";
        private const string AutomaticBanCreator = "system:auto-admin-login";
        public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(20);

        public async Task<AdminLoginProtectionResult> RegisterFailedAttemptAsync(
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            string? normalizedIpAddress = NormalizeIpAddress(ipAddress);
            if(normalizedIpAddress is null || IsProtectedSystemIp(normalizedIpAddress))
            {
                return new AdminLoginProtectionResult(false, 0, null);
            }

            DateTime now = DateTime.UtcNow;

            AdminLoginFailure? failure = await dbContext.AdminLoginFailures
                                                        .SingleOrDefaultAsync(
                                                            entry => entry.IpAddress == normalizedIpAddress,
                                                            cancellationToken);

            if(failure is null)
            {
                failure = new AdminLoginFailure
                {
                    IpAddress = normalizedIpAddress,
                    FailedAttempts = 1,
                    FirstFailedAt = now,
                    LastFailedAt = now,
                    UpdatedAt = now
                };

                dbContext.AdminLoginFailures.Add(failure);
            }
            else if(now - failure.FirstFailedAt > FailureWindow)
            {
                failure.FailedAttempts = 1;
                failure.FirstFailedAt = now;
                failure.LastFailedAt = now;
                failure.UpdatedAt = now;
            }
            else
            {
                failure.FailedAttempts++;
                failure.LastFailedAt = now;
                failure.UpdatedAt = now;
            }

            if(failure.FailedAttempts < FailureThreshold)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return new AdminLoginProtectionResult(false, failure.FailedAttempts, null);
            }

            bool hasActiveBan = await dbContext.IpBans.AnyAsync(
                entry => entry.IpAddress == normalizedIpAddress &&
                         entry.IsActive &&
                         entry.RevokedAt == null &&
                         (entry.ExpiresAt == null || entry.ExpiresAt > now),
                cancellationToken);

            DateTime banExpiresAt = now.Add(FailureWindow);

            if(!hasActiveBan)
            {
                dbContext.IpBans.Add(new IpBan
                {
                    IpAddress = normalizedIpAddress,
                    Reason = AutomaticBanReason,
                    ExpiresAt = banExpiresAt,
                    DurationMinutes = (int)FailureWindow.TotalMinutes,
                    CreatedBy = AutomaticBanCreator,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                });
            }

            dbContext.AdminLoginFailures.Remove(failure);

            await dbContext.SaveChangesAsync(cancellationToken);
            await cacheInvalidationService.InvalidateIpBansAsync(normalizedIpAddress, cancellationToken);

            return new AdminLoginProtectionResult(true, FailureThreshold, banExpiresAt);
        }

        public async Task ResetFailuresAsync(
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            string? normalizedIpAddress = NormalizeIpAddress(ipAddress);
            if(normalizedIpAddress is null || IsProtectedSystemIp(normalizedIpAddress))
            {
                return;
            }

            AdminLoginFailure? failure = await dbContext.AdminLoginFailures
                                                        .SingleOrDefaultAsync(
                                                            entry => entry.IpAddress == normalizedIpAddress,
                                                            cancellationToken);

            if(failure is null)
            {
                return;
            }

            dbContext.AdminLoginFailures.Remove(failure);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string? NormalizeIpAddress(string? ipAddress)
        {
            if(string.IsNullOrWhiteSpace(ipAddress))
            {
                return null;
            }

            if(!IPAddress.TryParse(ipAddress.Trim(), out IPAddress? parsedAddress))
            {
                return null;
            }

            return parsedAddress.IsIPv4MappedToIPv6 ? parsedAddress.MapToIPv4().ToString() : parsedAddress.ToString();
        }

        private static bool IsProtectedSystemIp(string ipAddress)
        {
            if(string.Equals(ipAddress, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if(!IPAddress.TryParse(ipAddress, out IPAddress? parsedAddress))
            {
                return false;
            }

            return IPAddress.IsLoopback(parsedAddress) ||
                   (parsedAddress.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(parsedAddress.MapToIPv4()));
        }
    }

    public sealed record AdminLoginProtectionResult(
        bool BanApplied,
        int FailedAttempts,
        DateTime? BanExpiresAt);
}
