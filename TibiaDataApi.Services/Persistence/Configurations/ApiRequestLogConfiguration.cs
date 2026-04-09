using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Monitoring;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class ApiRequestLogConfiguration : IEntityTypeConfiguration<ApiRequestLog>
    {
        public void Configure(EntityTypeBuilder<ApiRequestLog> builder)
        {
            builder.ToTable("api_request_logs");

            builder.HasKey(entry => entry.Id);

            builder.Property(entry => entry.IpAddress)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(entry => entry.Method)
                   .IsRequired()
                   .HasMaxLength(16);

            builder.Property(entry => entry.Route)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(entry => entry.DurationMs)
                   .HasProviderDoubleColumnType();

            builder.Property(entry => entry.UserAgent)
                   .HasMaxLength(512);

            builder.Property(entry => entry.CacheStatus)
                   .IsRequired()
                   .HasMaxLength(32);

            builder.Property(entry => entry.OccurredAt)
                   .HasProviderDateTimeColumnType();

            builder.HasIndex(entry => entry.OccurredAt);
            builder.HasIndex(entry => entry.IpAddress);
            builder.HasIndex(entry => entry.CacheStatus);
            builder.HasIndex(entry => new
            {
                entry.Method,
                entry.Route
            });
            builder.HasIndex(entry => entry.IsBlocked);
        }
    }
}
