using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Scraping;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public class ScrapeLogConfiguration : IEntityTypeConfiguration<ScrapeLog>
    {
        public void Configure(EntityTypeBuilder<ScrapeLog> builder)
        {
            builder.ToTable("scrape_logs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.TriggeredBy)
                   .HasMaxLength(100);

            builder.Property(x => x.ScraperName)
                   .HasMaxLength(200);

            builder.Property(x => x.CategoryName)
                   .HasMaxLength(200);

            builder.Property(x => x.CategorySlug)
                   .HasMaxLength(150);

            builder.Property(x => x.ErrorMessage)
                   .HasMaxLength(2000);

            builder.Property(x => x.ErrorType)
                   .HasMaxLength(200);

            builder.Property(x => x.ChangesJson)
                   .HasProviderJsonColumnType();

            builder.Property(x => x.MetadataJson)
                   .HasProviderJsonColumnType();

            builder.HasIndex(x => x.StartedAt);
            builder.HasIndex(x => x.Status);

            builder.HasMany(x => x.ItemChanges)
                   .WithOne(x => x.ScrapeLog)
                   .HasForeignKey(x => x.ScrapeLogId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Errors)
                   .WithOne(x => x.ScrapeLog)
                   .HasForeignKey(x => x.ScrapeLogId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
