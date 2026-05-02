using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.Persistence.Configurations
{
       public sealed class GemModifierGradeConfiguration : IEntityTypeConfiguration<GemModifierGrade>
       {
              public void Configure(EntityTypeBuilder<GemModifierGrade> builder)
              {
                     builder.ToTable("gem_modifier_grades");

                     builder.HasKey(entry => entry.Id);

                     builder.Property(entry => entry.GemModifierId)
                            .IsRequired();

                     builder.Property(entry => entry.Grade)
                            .IsRequired();

                     builder.Property(entry => entry.ValueText)
                            .IsRequired()
                            .HasMaxLength(500);

                     builder.Property(entry => entry.ValueNumeric)
                            .HasPrecision(18, 6);

                     builder.Property(entry => entry.IsIncomplete)
                            .IsRequired();

                     builder.Property(entry => entry.LastUpdated)
                            .HasProviderDateTimeColumnType();

                     builder.HasOne(entry => entry.GemModifier)
                            .WithMany(modifier => modifier.Grades)
                            .HasForeignKey(entry => entry.GemModifierId)
                            .OnDelete(DeleteBehavior.Cascade);

                     builder.HasIndex(entry => new
                            {
                                   entry.GemModifierId,
                                   entry.Grade
                            })
                            .IsUnique();
              }
       }
}
