using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Monitoring;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class BackgroundJobExecutionConfiguration : IEntityTypeConfiguration<BackgroundJobExecution>
    {
        public void Configure(EntityTypeBuilder<BackgroundJobExecution> builder)
        {
            builder.ToTable("background_job_executions");

            builder.HasKey(entry => entry.Id);

            builder.Property(entry => entry.JobName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(entry => entry.TriggeredBy)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(entry => entry.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(entry => entry.LeaseName)
                   .HasMaxLength(150);

            builder.Property(entry => entry.LeaseOwnerId)
                   .HasMaxLength(100);

            builder.Property(entry => entry.Message)
                   .HasMaxLength(2000);

            builder.Property(entry => entry.MetadataJson)
                   .HasProviderJsonColumnType();

            builder.HasIndex(entry => entry.JobName);
            builder.HasIndex(entry => entry.StartedAt);
            builder.HasIndex(entry => entry.Status);
            builder.HasIndex(entry => new
            {
                entry.JobName,
                entry.StartedAt
            });
        }
    }
}
