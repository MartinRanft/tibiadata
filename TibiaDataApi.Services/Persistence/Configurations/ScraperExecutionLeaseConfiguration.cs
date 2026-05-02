using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Scraping;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class ScraperExecutionLeaseConfiguration : IEntityTypeConfiguration<ScraperExecutionLease>
    {
        public void Configure(EntityTypeBuilder<ScraperExecutionLease> builder)
        {
            builder.ToTable("scraper_execution_leases");

            builder.HasKey(x => x.Name);

            builder.Property(x => x.Name)
                   .HasMaxLength(150);

            builder.Property(x => x.OwnerId)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasIndex(x => x.ExpiresAt);
            builder.HasIndex(x => x.UpdatedAt);
        }
    }
}