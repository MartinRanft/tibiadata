using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class WheelPerkStageConfiguration : IEntityTypeConfiguration<WheelPerkStage>
    {
        public void Configure(EntityTypeBuilder<WheelPerkStage> builder)
        {
            builder.ToTable("wheel_perk_stages");

            builder.HasKey(entry => entry.Id);

            builder.Property(entry => entry.EffectSummary)
                   .HasMaxLength(2000);

            builder.Property(entry => entry.EffectDetailsJson)
                   .HasProviderJsonColumnType();

            builder.HasIndex(entry => new
                   {
                       entry.WheelPerkId,
                       entry.Stage
                   })
                   .IsUnique();

            builder.HasIndex(entry => new
                   {
                       entry.WheelPerkId,
                       entry.SortOrder
                   });

            builder.HasOne(entry => entry.WheelPerk)
                   .WithMany(entry => entry.Stages)
                   .HasForeignKey(entry => entry.WheelPerkId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
