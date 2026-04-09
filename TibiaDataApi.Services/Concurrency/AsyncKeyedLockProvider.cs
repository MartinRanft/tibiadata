using System.Collections.Concurrent;

namespace TibiaDataApi.Services.Concurrency
{
    internal static class AsyncKeyedLockProvider
    {
        private static readonly ConcurrentDictionary<string, LockEntry> Entries = new(StringComparer.OrdinalIgnoreCase);

        public static async ValueTask<IDisposable> AcquireAsync(
            string scope,
            string key,
            CancellationToken cancellationToken = default)
        {
            string normalizedScope = string.IsNullOrWhiteSpace(scope) ? "default" : scope.Trim();
            string normalizedKey = string.IsNullOrWhiteSpace(key) ? "default" : key.Trim();
            string entryKey = $"{normalizedScope}:{normalizedKey}";

            LockEntry entry = Entries.AddOrUpdate(
                entryKey,
                _ => new LockEntry(),
                (_, existing) =>
                {
                    existing.AddReference();
                    return existing;
                });

            try
            {
                await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                return new Releaser(entryKey, entry);
            }
            catch
            {
                ReleaseReference(entryKey, entry);
                throw;
            }
        }

        private static void ReleaseReference(string entryKey, LockEntry entry)
        {
            if(!entry.ReleaseReference())
            {
                return;
            }

            Entries.TryRemove(new KeyValuePair<string, LockEntry>(entryKey, entry));
            entry.Semaphore.Dispose();
        }

        private sealed class LockEntry
        {
            private int _referenceCount = 1;

            public SemaphoreSlim Semaphore { get; } = new(1, 1);

            public void AddReference()
            {
                Interlocked.Increment(ref _referenceCount);
            }

            public bool ReleaseReference()
            {
                return Interlocked.Decrement(ref _referenceCount) == 0;
            }
        }

        private sealed class Releaser(string entryKey, LockEntry entry) : IDisposable
        {
            private readonly LockEntry _entry = entry;
            private readonly string _entryKey = entryKey;
            private int _disposed;

            public void Dispose()
            {
                if(Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                _entry.Semaphore.Release();
                ReleaseReference(_entryKey, _entry);
            }
        }
    }
}