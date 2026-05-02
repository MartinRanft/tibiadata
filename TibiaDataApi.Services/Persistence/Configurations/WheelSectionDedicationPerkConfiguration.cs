using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class WheelSectionDedicationPerkConfiguration : IEntityTypeConfiguration<WheelSectionDedicationPerk>
    {
        public void Configure(EntityTypeBuilder<WheelSectionDedicationPerk> builder)
        {
            builder.ToTable("wheel_section_dedication_perks");

            builder.HasKey(entry => entry.Id);

            builder.HasIndex(entry => new
                   {
                       entry.WheelSectionId,
                       entry.SortOrder
                   })
                   .IsUnique();

            builder.HasIndex(entry => new
                   {
                       entry.WheelSectionId,
                       entry.WheelPerkId
                   })
                   .IsUnique();

            builder.HasOne(entry => entry.WheelSection)
                   .WithMany(entry => entry.DedicationPerks)
                   .HasForeignKey(entry => entry.WheelSectionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(entry => entry.WheelPerk)
                   .WithMany()
                   .HasForeignKey(entry => entry.WheelPerkId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
