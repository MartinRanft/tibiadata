using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Assets;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public class AssetConfiguration : IEntityTypeConfiguration<Asset>
    {
        public void Configure(EntityTypeBuilder<Asset> builder)
        {
            builder.ToTable("assets");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StorageKey)
                   .IsRequired()
                   .HasMaxLength(512);

            builder.Property(x => x.FileName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.SourcePageTitle).HasMaxLength(255);
            builder.Property(x => x.SourceFileTitle).HasMaxLength(255);
            builder.Property(x => x.SourceUrl).HasMaxLength(1024);
            builder.Property(x => x.MimeType).HasMaxLength(128);
            builder.Property(x => x.Extension).HasMaxLength(32);
            builder.Property(x => x.SourceSha1).HasMaxLength(64);
            builder.Property(x => x.ContentSha256).HasMaxLength(128);

            builder.HasIndex(x => x.StorageKey).IsUnique();
            builder.HasIndex(x => x.ContentSha256);
        }
    }
}