using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Assets;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class ItemImageSyncQueueEntryConfiguration : IEntityTypeConfiguration<ItemImageSyncQueueEntry>
    {
        public void Configure(EntityTypeBuilder<ItemImageSyncQueueEntry> builder)
        {
            builder.ToTable("item_image_sync_queue");

            builder.HasKey(x => x.ItemId);

            builder.Property(x => x.WikiPageTitle)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(x => x.ErrorMessage)
                   .HasMaxLength(2000);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.RequestedAt);
            builder.HasIndex(x => x.UpdatedAt);

            builder.HasOne(x => x.Item)
                   .WithOne(x => x.ImageSyncQueueEntry)
                   .HasForeignKey<ItemImageSyncQueueEntry>(x => x.ItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}