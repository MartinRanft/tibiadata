using System.Data.Common;
using System.Text;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TibiaDataApi.Services.Admin.Statistics
{
    public interface IDatabaseLoadMonitor
    {
        DatabaseLoadSnapshot GetSnapshot();

        void RecordCommand(string commandText, double durationMs, bool failed, DateTime occurredAtUtc);
    }

    public sealed record DatabaseLoadSnapshot(
        DateTime CollectedAtUtc,
        int WindowMinutes,
        int TotalCommands,
        double AverageDurationMs,
        double MaxDurationMs,
        int SlowCommandCount,
        int FailedCommandCount,
        double CommandsPerMinute,
        IReadOnlyList<DatabaseLoadCommandStat> TopCommands,
        IReadOnlyList<DatabaseLoadCommandSample> RecentSlowCommands);

    public sealed record DatabaseLoadCommandStat(
        string CommandText,
        int Count,
        double AverageDurationMs,
        double MaxDurationMs,
        int FailedCount,
        DateTime LastSeenAtUtc);

    public sealed record DatabaseLoadCommandSample(
        DateTime OccurredAtUtc,
        string CommandText,
        double DurationMs,
        bool Failed);

    public sealed class DatabaseLoadMonitor : IDatabaseLoadMonitor
    {
        private const double SlowQueryThresholdMs = 250d;
        private const int RollingWindowMinutes = 15;
        private const int MaxTrackedCommands = 4000;
        private readonly Queue<DatabaseCommandObservation> _observations = new();
        private readonly Lock _sync = new();

        public DatabaseLoadSnapshot GetSnapshot()
        {
            lock (_sync)
            {
                TrimExpired(DateTime.UtcNow);

                List<DatabaseCommandObservation> window = _observations.ToList();
                int totalCommands = window.Count;
                double averageDurationMs = totalCommands == 0 ? 0 : window.Average(entry => entry.DurationMs);
                double maxDurationMs = totalCommands == 0 ? 0 : window.Max(entry => entry.DurationMs);
                int slowCommandCount = window.Count(entry => entry.DurationMs >= SlowQueryThresholdMs);
                int failedCommandCount = window.Count(entry => entry.Failed);
                double commandsPerMinute = totalCommands / (double)RollingWindowMinutes;

                List<DatabaseLoadCommandStat> topCommands = window
                                                            .GroupBy(entry => entry.CommandText, StringComparer.Ordinal)
                                                            .Select(group => new DatabaseLoadCommandStat(
                                                                group.Key,
                                                                group.Count(),
                                                                group.Average(entry => entry.DurationMs),
                                                                group.Max(entry => entry.DurationMs),
                                                                group.Count(entry => entry.Failed),
                                                                group.Max(entry => entry.OccurredAtUtc)))
                                                            .OrderByDescending(entry => entry.Count)
                                                            .ThenByDescending(entry => entry.AverageDurationMs)
                                                            .ThenBy(entry => entry.CommandText, StringComparer.Ordinal)
                                                            .Take(12)
                                                            .ToList();

                List<DatabaseLoadCommandSample> recentSlowCommands = window
                                                                     .Where(entry => entry.DurationMs >= SlowQueryThresholdMs)
                                                                     .OrderByDescending(entry => entry.OccurredAtUtc)
                                                                     .ThenByDescending(entry => entry.DurationMs)
                                                                     .Take(15)
                                                                     .Select(entry => new DatabaseLoadCommandSample(
                                                                         entry.OccurredAtUtc,
                                                                         entry.CommandText,
                                                                         entry.DurationMs,
                                                                         entry.Failed))
                                                                     .ToList();

                return new DatabaseLoadSnapshot(
                    DateTime.UtcNow,
                    RollingWindowMinutes,
                    totalCommands,
                    averageDurationMs,
                    maxDurationMs,
                    slowCommandCount,
                    failedCommandCount,
                    commandsPerMinute,
                    topCommands,
                    recentSlowCommands);
            }
        }

        public void RecordCommand(string commandText, double durationMs, bool failed, DateTime occurredAtUtc)
        {
            DatabaseCommandObservation observation = new(
                NormalizeCommandText(commandText),
                Math.Max(0, durationMs),
                failed,
                occurredAtUtc);

            lock (_sync)
            {
                _observations.Enqueue(observation);
                TrimExpired(observation.OccurredAtUtc);

                while (_observations.Count > MaxTrackedCommands)
                {
                    _observations.Dequeue();
                }
            }
        }

        private void TrimExpired(DateTime nowUtc)
        {
            DateTime cutoff = nowUtc.AddMinutes(-RollingWindowMinutes);

            while (_observations.Count > 0 && _observations.Peek().OccurredAtUtc < cutoff)
            {
                _observations.Dequeue();
            }
        }

        private static string NormalizeCommandText(string? commandText)
        {
            if(string.IsNullOrWhiteSpace(commandText))
            {
                return "(empty command)";
            }

            StringBuilder builder = new(commandText.Length);
            bool previousWhitespace = false;

            foreach(char character in commandText)
            {
                if(char.IsWhiteSpace(character))
                {
                    if(previousWhitespace)
                    {
                        continue;
                    }

                    builder.Append(' ');
                    previousWhitespace = true;
                    continue;
                }

                builder.Append(character);
                previousWhitespace = false;
            }

            string normalized = builder.ToString().Trim();

            if(normalized.Length <= 240)
            {
                return normalized;
            }

            return normalized[..237] + "...";
        }

        private sealed record DatabaseCommandObservation(
            string CommandText,
            double DurationMs,
            bool Failed,
            DateTime OccurredAtUtc);
    }

    internal sealed class DatabaseCommandMetricsInterceptor(IDatabaseLoadMonitor databaseLoadMonitor) : DbCommandInterceptor
    {
        private readonly IDatabaseLoadMonitor _databaseLoadMonitor = databaseLoadMonitor;

        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            Record(command, eventData.Duration, false);
            return result;
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            Record(command, eventData.Duration, false);
            return new ValueTask<DbDataReader>(result);
        }

        public override object? ScalarExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            object? result)
        {
            Record(command, eventData.Duration, false);
            return result;
        }

        public override ValueTask<object?> ScalarExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            object? result,
            CancellationToken cancellationToken = default)
        {
            Record(command, eventData.Duration, false);
            return new ValueTask<object?>(result);
        }

        public override int NonQueryExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result)
        {
            Record(command, eventData.Duration, false);
            return result;
        }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            Record(command, eventData.Duration, false);
            return new ValueTask<int>(result);
        }

        public override void CommandFailed(
            DbCommand command,
            CommandErrorEventData eventData)
        {
            Record(command, eventData.Duration, true);
        }

        public override Task CommandFailedAsync(
            DbCommand command,
            CommandErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            Record(command, eventData.Duration, true);
            return Task.CompletedTask;
        }

        private void Record(DbCommand command, TimeSpan duration, bool failed)
        {
            _databaseLoadMonitor.RecordCommand(command.CommandText, duration.TotalMilliseconds, failed, DateTime.UtcNow);
        }
    }
}
