using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Monitoring;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class ApiRequestDailyAggregateConfiguration : IEntityTypeConfiguration<ApiRequestDailyAggregate>
    {
        public void Configure(EntityTypeBuilder<ApiRequestDailyAggregate> builder)
        {
            builder.ToTable("api_request_daily_aggregates");

            builder.HasKey(entry => entry.Id);

            builder.Property(entry => entry.DayUtc)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.TotalDurationMs)
                   .HasProviderDoubleColumnType();

            builder.Property(entry => entry.UpdatedAt)
                   .HasProviderDateTimeColumnType();

            builder.HasIndex(entry => entry.DayUtc)
                   .IsUnique();
        }
    }
}
