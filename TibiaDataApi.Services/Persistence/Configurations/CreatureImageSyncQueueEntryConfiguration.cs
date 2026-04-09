using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Assets;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class CreatureImageSyncQueueEntryConfiguration : IEntityTypeConfiguration<CreatureImageSyncQueueEntry>
    {
        public void Configure(EntityTypeBuilder<CreatureImageSyncQueueEntry> builder)
        {
            builder.ToTable("creature_image_sync_queue");

            builder.HasKey(x => x.CreatureId);

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

            builder.HasOne(x => x.Creature)
                   .WithOne(x => x.ImageSyncQueueEntry)
                   .HasForeignKey<CreatureImageSyncQueueEntry>(x => x.CreatureId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}