using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Scraping;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public class ScrapeErrorConfiguration : IEntityTypeConfiguration<ScrapeError>
    {
        public void Configure(EntityTypeBuilder<ScrapeError> builder)
        {
            builder.ToTable("scrape_errors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Scope)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.PageTitle)
                   .HasMaxLength(255);

            builder.Property(x => x.ItemName)
                   .HasMaxLength(255);

            builder.Property(x => x.ErrorType)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.Message)
                   .IsRequired()
                   .HasMaxLength(2000);

            builder.Property(x => x.DetailsJson)
                   .HasProviderJsonColumnType();

            builder.HasIndex(x => x.ScrapeLogId);
            builder.HasIndex(x => x.Scope);
            builder.HasIndex(x => x.OccurredAt);
        }
    }
}
