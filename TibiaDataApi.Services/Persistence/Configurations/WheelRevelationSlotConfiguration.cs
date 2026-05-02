using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class WheelRevelationSlotConfiguration : IEntityTypeConfiguration<WheelRevelationSlot>
    {
        public void Configure(EntityTypeBuilder<WheelRevelationSlot> builder)
        {
            builder.ToTable("wheel_revelation_slots");

            builder.HasKey(entry => entry.Id);

            builder.Property(entry => entry.SlotKey)
                   .IsRequired()
                   .HasMaxLength(8);

            builder.HasIndex(entry => new
                   {
                       entry.Vocation,
                       entry.SlotKey
                   })
                   .IsUnique();

            builder.HasIndex(entry => entry.WheelPerkId);

            builder.HasOne(entry => entry.WheelPerk)
                   .WithMany()
                   .HasForeignKey(entry => entry.WheelPerkId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(entry => entry.WheelPerkOccurrence)
                   .WithMany()
                   .HasForeignKey(entry => entry.WheelPerkOccurrenceId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
