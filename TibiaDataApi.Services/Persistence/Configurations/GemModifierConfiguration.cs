using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.Persistence.Configurations
{
       public sealed class GemModifierConfiguration : IEntityTypeConfiguration<GemModifier>
       {
              public void Configure(EntityTypeBuilder<GemModifier> builder)
              {
                     builder.ToTable("gem_modifiers");

                     builder.HasKey(entry => entry.Id);

                     builder.Property(entry => entry.Name)
                            .IsRequired()
                            .HasMaxLength(255);

                     builder.Property(entry => entry.VariantKey)
                            .IsRequired()
                            .HasMaxLength(200);

                     builder.Property(entry => entry.WikiUrl)
                            .IsRequired()
                            .HasMaxLength(500);

                     builder.Property(entry => entry.ModifierType)
                            .IsRequired();

                     builder.Property(entry => entry.Category)
                            .IsRequired();

                     builder.Property(entry => entry.VocationRestriction);

                     builder.Property(entry => entry.IsComboMod)
                            .IsRequired();

                     builder.Property(entry => entry.HasTradeoff)
                            .IsRequired();

                     builder.Property(entry => entry.Description)
                            .HasMaxLength(2000);

                     builder.Property(entry => entry.LastUpdated)
                            .HasProviderDateTimeColumnType();

                     builder.HasIndex(entry => new
                     {
                            entry.ModifierType,
                            entry.VariantKey
                     })
                            .IsUnique();

                     builder.HasIndex(entry => new
                     {
                            entry.Name,
                            entry.ModifierType,
                            entry.VocationRestriction
                     });

                     builder.HasIndex(entry => new
                     {
                            entry.ModifierType,
                            entry.Category
                     });

                     builder.HasIndex(entry => entry.VocationRestriction);

                     builder.Navigation(entry => entry.Grades)
                            .AutoInclude(false);
              }
       }
}
