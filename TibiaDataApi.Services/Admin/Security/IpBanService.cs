using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Entities.Security;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Admin.Security
{
    public sealed class IpBanService(
        TibiaDbContext dbContext,
        HybridCache hybridCache,
        CachingOptions cachingOptions,
        ICacheInvalidationService cacheInvalidationService) : IIpBanService
    {
        private readonly ICacheInvalidationService _cacheInvalidationService = cacheInvalidationService;
        private readonly HybridCacheEntryOptions _cacheOptions = cachingOptions.IpBan.ToEntryOptions();
        private readonly TibiaDbContext _dbContext = dbContext;
        private readonly HybridCache _hybridCache = hybridCache;

        public async Task<bool> IsBlockedAsync(
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            string? normalizedIp = NormalizeIpAddress(ipAddress);
            if(normalizedIp is null || IsProtectedSystemIp(normalizedIp))
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;

            return await _hybridCache.GetOrCreateAsync(
                $"ip-ban:blocked:{normalizedIp}",
                async cancellationToken => await _dbContext.IpBans
                                                           .AsNoTracking()
                                                           .AnyAsync(entry =>
                                                               entry.IpAddress == normalizedIp &&
                                                               entry.IsActive &&
                                                               entry.RevokedAt == null &&
                                                               (entry.ExpiresAt == null || entry.ExpiresAt > now),
                                                               cancellationToken),
                _cacheOptions,
                [CacheTags.IpBans, CacheTags.IpBanAddress(normalizedIp)],
                cancellationToken);
        }

        public async Task<IpBanPage> GetBansAsync(
            bool includeExpired = false,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            (int normalizedPage, int normalizedPageSize) = NormalizePagination(page, pageSize, 500);
            DateTime now = DateTime.UtcNow;

            IQueryable<IpBan> query = _dbContext.IpBans
                                                .AsNoTracking()
                                                .OrderByDescending(entry => entry.IsActive)
                                                .ThenByDescending(entry => entry.CreatedAt)
                                                .ThenByDescending(entry => entry.Id);

            if(!includeExpired)
            {
                query = query.Where(entry =>
                entry.IsActive &&
                entry.RevokedAt == null &&
                (entry.ExpiresAt == null || entry.ExpiresAt > now));
            }

            return await _hybridCache.GetOrCreateAsync(
                $"ip-ban:list:{includeExpired}:{normalizedPage}:{normalizedPageSize}",
                async cancellationToken =>
                {
                    int totalCount = await query.CountAsync(cancellationToken);

                    List<IpBanEntry> items = await query
                                                   .Skip((normalizedPage - 1) * normalizedPageSize)
                                                   .Take(normalizedPageSize)
                                                   .Select(entry => new IpBanEntry(
                                                       entry.IpAddress,
                                                       entry.Reason,
                                                       entry.IsActive &&
                                                       entry.RevokedAt == null &&
                                                       (entry.ExpiresAt == null || entry.ExpiresAt > now),
                                                       entry.CreatedAt,
                                                       entry.ExpiresAt,
                                                       entry.DurationMinutes,
                                                       entry.CreatedBy))
                                                   .ToListAsync(cancellationToken);

                    return new IpBanPage(normalizedPage, normalizedPageSize, totalCount, items);
                },
                _cacheOptions,
                [CacheTags.IpBans],
                cancellationToken);
        }

        public async Task<IpBanMutationResult> BanAsync(
            IpBanMutationRequest request,
            CancellationToken cancellationToken = default)
        {
            string? normalizedIp = NormalizeIpAddress(request.IpAddress);
            if(normalizedIp is null)
            {
                return new IpBanMutationResult(IpBanMutationOutcome.InvalidIp, "Invalid IP address.", request.IpAddress);
            }

            if(IsProtectedSystemIp(normalizedIp))
            {
                return new IpBanMutationResult(
                    IpBanMutationOutcome.ProtectedIp,
                    "Loopback and localhost addresses cannot be banned.",
                    normalizedIp);
            }

            DateTime now = DateTime.UtcNow;
            (DateTime? expiresAt, int? durationMinutes, string? validationMessage) = ResolveBanWindow(
                request.ExpiresAt,
                request.DurationMinutes,
                now);

            if(validationMessage is not null)
            {
                return new IpBanMutationResult(IpBanMutationOutcome.InvalidBanWindow, validationMessage, normalizedIp);
            }

            bool exists = await _dbContext.IpBans.AnyAsync(entry =>
                entry.IpAddress == normalizedIp &&
                entry.IsActive &&
                entry.RevokedAt == null &&
                (entry.ExpiresAt == null || entry.ExpiresAt > now),
                cancellationToken);

            if(exists)
            {
                return new IpBanMutationResult(IpBanMutationOutcome.AlreadyExists, "The IP address is already banned.", normalizedIp);
            }

            _dbContext.IpBans.Add(new IpBan
            {
                IpAddress = normalizedIp,
                Reason = request.Reason.Trim(),
                ExpiresAt = expiresAt,
                DurationMinutes = durationMinutes,
                CreatedBy = NormalizeOptional(request.CreatedBy),
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cacheInvalidationService.InvalidateIpBansAsync(normalizedIp, cancellationToken);

            return new IpBanMutationResult(IpBanMutationOutcome.Success, "The IP address has been banned.", normalizedIp);
        }

        public async Task<IpBanMutationResult> UnbanAsync(
            IpBanRemovalRequest request,
            CancellationToken cancellationToken = default)
        {
            string? normalizedIp = NormalizeIpAddress(request.IpAddress);
            if(normalizedIp is null)
            {
                return new IpBanMutationResult(IpBanMutationOutcome.InvalidIp, "Invalid IP address.", request.IpAddress);
            }

            DateTime now = DateTime.UtcNow;

            List<IpBan> activeBans = await _dbContext.IpBans
                                                     .Where(entry =>
                                                     entry.IpAddress == normalizedIp &&
                                                     entry.IsActive &&
                                                     entry.RevokedAt == null &&
                                                     (entry.ExpiresAt == null || entry.ExpiresAt > now))
                                                     .ToListAsync(cancellationToken);

            if(activeBans.Count == 0)
            {
                return new IpBanMutationResult(IpBanMutationOutcome.NotFound, "No active ban exists for the requested IP address.", normalizedIp);
            }

            foreach(IpBan ban in activeBans)
            {
                ban.IsActive = false;
                ban.RevokedAt = now;
                ban.RevokedBy = NormalizeOptional(request.RequestedBy);
                ban.RevocationReason = NormalizeOptional(request.Reason);
                ban.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cacheInvalidationService.InvalidateIpBansAsync(normalizedIp, cancellationToken);

            return new IpBanMutationResult(IpBanMutationOutcome.Success, "The IP address has been unbanned.", normalizedIp);
        }

        private static string? NormalizeIpAddress(string? ipAddress)
        {
            if(string.IsNullOrWhiteSpace(ipAddress))
            {
                return null;
            }

            if(!IPAddress.TryParse(ipAddress.Trim(), out IPAddress? parsed))
            {
                return null;
            }

            if(parsed.IsIPv4MappedToIPv6)
            {
                return parsed.ToString();
            }

            return parsed.ToString();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static (DateTime? ExpiresAt, int? DurationMinutes, string? ValidationMessage) ResolveBanWindow(
            DateTime? expiresAt,
            int? durationMinutes,
            DateTime now)
        {
            if(durationMinutes.HasValue && durationMinutes.Value <= 0)
            {
                return (null, null, "DurationMinutes must be greater than zero when specified.");
            }

            if(durationMinutes.HasValue && expiresAt.HasValue)
            {
                return (null, null, "Specify either ExpiresAt or DurationMinutes, not both.");
            }

            if(durationMinutes.HasValue)
            {
                DateTime resolvedExpiresAt = now.AddMinutes(durationMinutes.Value);
                return (resolvedExpiresAt, durationMinutes.Value, null);
            }

            if(expiresAt.HasValue)
            {
                if(expiresAt.Value <= now)
                {
                    return (null, null, "ExpiresAt must be in the future.");
                }

                int resolvedDurationMinutes = (int)Math.Ceiling((expiresAt.Value - now).TotalMinutes);
                return (expiresAt.Value, resolvedDurationMinutes, null);
            }

            return (null, null, null);
        }

        private static bool IsProtectedSystemIp(string ipAddress)
        {
            if(string.Equals(ipAddress, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if(!IPAddress.TryParse(ipAddress, out IPAddress? parsed))
            {
                return false;
            }

            return IPAddress.IsLoopback(parsed) ||
                   (parsed.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(parsed.MapToIPv4()));
        }

        private static (int Page, int PageSize) NormalizePagination(int page, int pageSize, int maxPageSize)
        {
            int normalizedPage = page < 1 ? 1 : page;
            int normalizedPageSize = pageSize < 1 ? 1 : pageSize;

            if(normalizedPageSize > maxPageSize)
            {
                normalizedPageSize = maxPageSize;
            }

            return (normalizedPage, normalizedPageSize);
        }
    }
}
