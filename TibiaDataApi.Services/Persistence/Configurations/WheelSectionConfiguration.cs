using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class WheelSectionConfiguration : IEntityTypeConfiguration<WheelSection>
    {
        public void Configure(EntityTypeBuilder<WheelSection> builder)
        {
            builder.ToTable("wheel_sections");

            builder.HasKey(entry => entry.Id);

            builder.Property(entry => entry.SectionKey)
                   .IsRequired()
                   .HasMaxLength(16);

            builder.HasIndex(entry => new
                   {
                       entry.Vocation,
                       entry.SectionKey
                   })
                   .IsUnique();

            builder.HasIndex(entry => new
                   {
                       entry.Vocation,
                       entry.Quarter,
                       entry.SortOrder
                   })
                   .IsUnique();

            builder.HasIndex(entry => entry.ConvictionWheelPerkId);

            builder.HasOne(entry => entry.ConvictionWheelPerk)
                   .WithMany()
                   .HasForeignKey(entry => entry.ConvictionWheelPerkId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(entry => entry.ConvictionWheelPerkOccurrence)
                   .WithMany()
                   .HasForeignKey(entry => entry.ConvictionWheelPerkOccurrenceId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.Navigation(entry => entry.DedicationPerks)
                   .AutoInclude(false);
        }
    }
}
