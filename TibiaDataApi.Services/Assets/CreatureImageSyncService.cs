using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MySql.Data.MySqlClient;

using TibiaDataApi.Services.BackgroundJobs;
using TibiaDataApi.Services.Concurrency;
using TibiaDataApi.Services.Entities.Assets;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Assets
{
    public sealed class CreatureImageSyncService(
        IServiceScopeFactory serviceScopeFactory,
        BackgroundJobOptions backgroundJobOptions,
        ILogger<CreatureImageSyncService> logger) : ICreatureImageSyncService
    {
        private readonly BackgroundJobOptions _backgroundJobOptions = backgroundJobOptions;
        private readonly ILogger<CreatureImageSyncService> _logger = logger;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        public async Task QueuePrimaryImageSyncAsync(
            int creatureId,
            string wikiPageTitle,
            bool forceSync,
            CancellationToken cancellationToken = default)
        {
            if(creatureId <= 0 || string.IsNullOrWhiteSpace(wikiPageTitle))
            {
                return;
            }

            using IDisposable queueLock = await AsyncKeyedLockProvider.AcquireAsync(
                "creature-image-sync-queue",
                creatureId.ToString(),
                cancellationToken).ConfigureAwait(false);

            await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            string normalizedWikiPageTitle = wikiPageTitle.Trim();

            bool hasPrimaryAsset = await db.CreatureAssets.AnyAsync(
                entry => entry.CreatureId == creatureId &&
                         entry.AssetKind == AssetKind.PrimaryImage &&
                         entry.IsPrimary,
                cancellationToken).ConfigureAwait(false);

            CreatureImageSyncQueueEntry? existingEntry = await db.CreatureImageSyncQueueEntries
                                                                 .FirstOrDefaultAsync(entry => entry.CreatureId == creatureId, cancellationToken)
                                                                 .ConfigureAwait(false);

            bool shouldQueue = forceSync || !hasPrimaryAsset || existingEntry is null ||
                               existingEntry.Status is ItemImageSyncState.Failed or ItemImageSyncState.Missing;

            if(existingEntry is null)
            {
                db.CreatureImageSyncQueueEntries.Add(new CreatureImageSyncQueueEntry
                {
                    CreatureId = creatureId,
                    WikiPageTitle = normalizedWikiPageTitle,
                    Status = shouldQueue ? ItemImageSyncState.Pending : ItemImageSyncState.Succeeded,
                    RequestedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                await SaveQueueEntryAsync(db, creatureId, normalizedWikiPageTitle, shouldQueue, cancellationToken).ConfigureAwait(false);
                return;
            }

            existingEntry.WikiPageTitle = normalizedWikiPageTitle;
            existingEntry.UpdatedAt = DateTime.UtcNow;

            if(!shouldQueue)
            {
                await SaveQueueEntryAsync(db, creatureId, normalizedWikiPageTitle, shouldQueue, cancellationToken).ConfigureAwait(false);
                return;
            }

            existingEntry.Status = ItemImageSyncState.Pending;
            existingEntry.RequestedAt = DateTime.UtcNow;
            existingEntry.ErrorMessage = null;

            await SaveQueueEntryAsync(db, creatureId, normalizedWikiPageTitle, shouldQueue, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CreatureImageSyncBatchResult> SyncPendingAsync(
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            int normalizedBatchSize = Math.Max(1, batchSize);
            List<ClaimedQueueEntry> claimedEntries = await ClaimPendingEntriesAsync(normalizedBatchSize, cancellationToken).ConfigureAwait(false);

            if(claimedEntries.Count == 0)
            {
                return new CreatureImageSyncBatchResult(0, 0, 0, 0, 0);
            }

            int maxParallelWorkers = GetMaxParallelWorkers(claimedEntries.Count);
            await Parallel.ForEachAsync(
                claimedEntries,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = maxParallelWorkers
                },
                async (entry, ct) =>
                {
                    SyncEntryResult result = await ProcessClaimedEntryAsync(entry, ct).ConfigureAwait(false);
                    entry.Result = result;
                }).ConfigureAwait(false);

            int succeeded = claimedEntries.Count(entry => entry.Result == SyncEntryResult.Succeeded);
            int missing = claimedEntries.Count(entry => entry.Result == SyncEntryResult.Missing);
            int failed = claimedEntries.Count(entry => entry.Result == SyncEntryResult.Failed);
            int skipped = claimedEntries.Count(entry => entry.Result == SyncEntryResult.Skipped);

            return new CreatureImageSyncBatchResult(claimedEntries.Count, succeeded, missing, failed, skipped);
        }

        private async Task<List<ClaimedQueueEntry>> ClaimPendingEntriesAsync(int batchSize, CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            List<CreatureImageSyncQueueEntry> queueEntries = await db.CreatureImageSyncQueueEntries
                                                                     .Where(entry => entry.Status == ItemImageSyncState.Pending)
                                                                     .OrderBy(entry => entry.RequestedAt)
                                                                     .Take(batchSize)
                                                                     .ToListAsync(cancellationToken).ConfigureAwait(false);

            if(queueEntries.Count == 0)
            {
                return [];
            }

            DateTime claimedAt = DateTime.UtcNow;

            foreach(CreatureImageSyncQueueEntry queueEntry in queueEntries)
            {
                queueEntry.Status = ItemImageSyncState.Processing;
                queueEntry.LastAttemptedAt = claimedAt;
                queueEntry.UpdatedAt = claimedAt;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return queueEntries
                   .Select(entry => new ClaimedQueueEntry(entry.CreatureId, entry.WikiPageTitle))
                   .ToList();
        }

        private async Task<SyncEntryResult> ProcessClaimedEntryAsync(
            ClaimedQueueEntry claimedEntry,
            CancellationToken cancellationToken)
        {
            using IDisposable queueLock = await AsyncKeyedLockProvider.AcquireAsync(
                "creature-image-sync-worker",
                claimedEntry.CreatureId.ToString(),
                cancellationToken).ConfigureAwait(false);

            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            ICreatureImageAssetService creatureImageAssetService = scope.ServiceProvider.GetRequiredService<ICreatureImageAssetService>();

            CreatureImageSyncQueueEntry? queueEntry = await db.CreatureImageSyncQueueEntries
                                                              .Include(entry => entry.Creature)
                                                              .FirstOrDefaultAsync(entry => entry.CreatureId == claimedEntry.CreatureId, cancellationToken)
                                                              .ConfigureAwait(false);

            if(queueEntry is null)
            {
                return SyncEntryResult.Skipped;
            }

            if(queueEntry.Creature is null)
            {
                db.CreatureImageSyncQueueEntries.Remove(queueEntry);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return SyncEntryResult.Skipped;
            }

            try
            {
                await creatureImageAssetService.SyncPrimaryImageAsync(
                    db,
                    queueEntry.Creature,
                    queueEntry.WikiPageTitle,
                    cancellationToken).ConfigureAwait(false);

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                bool hasPrimaryAsset = await db.CreatureAssets.AnyAsync(
                    entry => entry.CreatureId == queueEntry.CreatureId &&
                             entry.AssetKind == AssetKind.PrimaryImage &&
                             entry.IsPrimary,
                    cancellationToken).ConfigureAwait(false);

                queueEntry.Status = hasPrimaryAsset ? ItemImageSyncState.Succeeded : ItemImageSyncState.Missing;
                queueEntry.LastCompletedAt = DateTime.UtcNow;
                queueEntry.ErrorMessage = null;
                queueEntry.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return hasPrimaryAsset ? SyncEntryResult.Succeeded : SyncEntryResult.Missing;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                queueEntry.Status = ItemImageSyncState.Failed;
                queueEntry.RetryCount++;
                queueEntry.ErrorMessage = ex.Message;
                queueEntry.UpdatedAt = DateTime.UtcNow;

                _logger.LogError(
                    ex,
                    "Creature image sync failed for creature {CreatureId} ({WikiPageTitle}).",
                    queueEntry.CreatureId,
                    queueEntry.WikiPageTitle);

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return SyncEntryResult.Failed;
            }
        }

        private int GetMaxParallelWorkers(int claimedEntryCount)
        {
            int configured = _backgroundJobOptions.CreatureImageSync.MaxParallelWorkers;
            return configured <= 0 ? Math.Max(1, claimedEntryCount) : Math.Clamp(configured, 1, Math.Max(1, claimedEntryCount));
        }

        private static bool IsDuplicatePrimaryKey(DbUpdateException exception)
        {
            return exception.InnerException is MySqlException mySqlException &&
                   mySqlException.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) &&
                   mySqlException.Message.Contains("PRIMARY", StringComparison.OrdinalIgnoreCase);
        }

        private async Task SaveQueueEntryAsync(
            TibiaDbContext db,
            int creatureId,
            string wikiPageTitle,
            bool shouldQueue,
            CancellationToken cancellationToken)
        {
            try
            {
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException ex) when (IsDuplicatePrimaryKey(ex))
            {
                db.ChangeTracker.Clear();

                CreatureImageSyncQueueEntry? existingEntry = await db.CreatureImageSyncQueueEntries
                                                                     .FirstOrDefaultAsync(entry => entry.CreatureId == creatureId, cancellationToken)
                                                                     .ConfigureAwait(false);

                if(existingEntry is null)
                {
                    throw;
                }

                existingEntry.WikiPageTitle = wikiPageTitle;
                existingEntry.UpdatedAt = DateTime.UtcNow;

                if(shouldQueue)
                {
                    existingEntry.Status = ItemImageSyncState.Pending;
                    existingEntry.RequestedAt = DateTime.UtcNow;
                    existingEntry.ErrorMessage = null;
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private sealed class ClaimedQueueEntry(int creatureId, string wikiPageTitle)
        {
            public int CreatureId { get; } = creatureId;

            public string WikiPageTitle { get; } = wikiPageTitle;

            public SyncEntryResult Result { get; set; }
        }

        private enum SyncEntryResult
        {
            Unknown = 0,
            Succeeded,
            Missing,
            Failed,
            Skipped
        }
    }
}