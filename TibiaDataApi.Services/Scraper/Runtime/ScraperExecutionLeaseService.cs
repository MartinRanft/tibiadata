using System.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Scraper.Runtime
{
    public sealed class ScraperExecutionLeaseService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ScraperExecutionLeaseService> logger) : IScraperExecutionLeaseService
    {
        private readonly ILogger<ScraperExecutionLeaseService> _logger = logger;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        public async Task<ScraperExecutionLeaseAcquireResult> TryAcquireAsync(
            string leaseName,
            string ownerId,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            IExecutionStrategy executionStrategy = db.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                DateTime now = DateTime.UtcNow;

                ScraperExecutionLease? lease = await db.ScraperExecutionLeases
                                                       .FirstOrDefaultAsync(entry => entry.Name == leaseName, cancellationToken);

                if(lease is null)
                {
                    db.ScraperExecutionLeases.Add(new ScraperExecutionLease
                    {
                        Name = leaseName,
                        OwnerId = ownerId,
                        AcquiredAt = now,
                        ExpiresAt = now.Add(leaseDuration),
                        UpdatedAt = now
                    });

                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return new ScraperExecutionLeaseAcquireResult(true, ownerId, now.Add(leaseDuration));
                }

                bool isExpired = lease.ExpiresAt <= now;
                bool isSameOwner = string.Equals(lease.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase);

                if(!isExpired && !isSameOwner)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new ScraperExecutionLeaseAcquireResult(false, lease.OwnerId, lease.ExpiresAt);
                }

                lease.OwnerId = ownerId;
                lease.AcquiredAt = now;
                lease.ExpiresAt = now.Add(leaseDuration);
                lease.UpdatedAt = now;

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new ScraperExecutionLeaseAcquireResult(true, ownerId, lease.ExpiresAt);
            });
        }

        public async Task<bool> RenewAsync(
            string leaseName,
            string ownerId,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            DateTime now = DateTime.UtcNow;
            ScraperExecutionLease? lease = await db.ScraperExecutionLeases
                                                   .FirstOrDefaultAsync(entry => entry.Name == leaseName, cancellationToken);

            if(lease is null ||
               !string.Equals(lease.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase) ||
               lease.ExpiresAt <= now)
            {
                return false;
            }

            lease.ExpiresAt = now.Add(leaseDuration);
            lease.UpdatedAt = now;

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task ReleaseAsync(
            string leaseName,
            string ownerId,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            ScraperExecutionLease? lease = await db.ScraperExecutionLeases
                                                   .FirstOrDefaultAsync(entry => entry.Name == leaseName, cancellationToken);

            if(lease is null)
            {
                return;
            }

            if(!string.Equals(lease.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Skipping scraper execution lease release for {LeaseName} because owner {OwnerId} no longer owns the lease.",
                    leaseName,
                    ownerId);
                return;
            }

            db.ScraperExecutionLeases.Remove(lease);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}