using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Scraping;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public class ScrapeItemChangeConfiguration : IEntityTypeConfiguration<ScrapeItemChange>
    {
        public void Configure(EntityTypeBuilder<ScrapeItemChange> builder)
        {
            builder.ToTable("scrape_item_changes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.ChangeType)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.CategorySlug)
                   .HasMaxLength(150);

            builder.Property(x => x.CategoryName)
                   .HasMaxLength(200);

            builder.Property(x => x.ChangedFieldsJson)
                   .HasProviderJsonColumnType();

            builder.Property(x => x.BeforeJson)
                   .HasProviderJsonColumnType();

            builder.Property(x => x.AfterJson)
                   .HasProviderJsonColumnType();

            builder.Property(x => x.ErrorMessage)
                   .HasMaxLength(2000);

            builder.HasIndex(x => x.ScrapeLogId);
            builder.HasIndex(x => x.ItemId);
            builder.HasIndex(x => x.ChangeType);
            builder.HasIndex(x => x.OccurredAt);
        }
    }
}
