using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class WheelPerkOccurrenceConfiguration : IEntityTypeConfiguration<WheelPerkOccurrence>
    {
        public void Configure(EntityTypeBuilder<WheelPerkOccurrence> builder)
        {
            builder.ToTable("wheel_perk_occurrences");

            builder.HasKey(entry => entry.Id);

            builder.Property(entry => entry.Notes)
                   .HasMaxLength(1000);

            builder.HasIndex(entry => new
                   {
                       entry.WheelPerkId,
                       entry.OccurrenceIndex
                   })
                   .IsUnique();

            builder.HasIndex(entry => new
                   {
                       entry.WheelPerkId,
                       entry.Domain
                   });

            builder.HasOne(entry => entry.WheelPerk)
                   .WithMany(entry => entry.Occurrences)
                   .HasForeignKey(entry => entry.WheelPerkId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
