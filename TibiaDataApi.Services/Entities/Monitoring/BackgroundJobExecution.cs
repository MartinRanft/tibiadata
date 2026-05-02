using TibiaDataApi.Services.BackgroundJobs;

namespace TibiaDataApi.Services.Entities.Monitoring
{
    public sealed class BackgroundJobExecution
    {
        public int Id { get; set; }

        public required string JobName { get; set; }

        public required string TriggeredBy { get; set; }

        public BackgroundJobExecutionState Status { get; set; } = BackgroundJobExecutionState.Running;

        public string? LeaseName { get; set; }

        public string? LeaseOwnerId { get; set; }

        public string? Message { get; set; }

        public int ProcessedCount { get; set; }

        public int SucceededCount { get; set; }

        public int FailedCount { get; set; }

        public int SkippedCount { get; set; }

        public string? MetadataJson { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? FinishedAt { get; set; }

        public double? DurationMs { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}