using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Monitoring;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class ScheduledScraperConfigurationConfiguration : IEntityTypeConfiguration<ScheduledScraperConfiguration>
    {
        public void Configure(EntityTypeBuilder<ScheduledScraperConfiguration> builder)
        {
            builder.ToTable("scheduled_scraper_configurations");

            builder.HasKey(entry => entry.Key);

            builder.Property(entry => entry.Key)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(entry => entry.LastTriggeredAtUtc)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.CreatedAt)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.UpdatedAt)
                   .HasProviderDateTimeColumnType();
        }
    }
}
