using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.Persistence.Configurations
{
       public sealed class GemConfiguration : IEntityTypeConfiguration<Gem>
       {
              public void Configure(EntityTypeBuilder<Gem> builder)
              {
                     builder.ToTable("gems");

                     builder.HasKey(entry => entry.Id);

                     builder.Property(entry => entry.Name)
                            .IsRequired()
                            .HasMaxLength(255);

                     builder.Property(entry => entry.WikiUrl)
                            .IsRequired()
                            .HasMaxLength(500);

                     builder.Property(entry => entry.GemFamily)
                            .IsRequired();

                     builder.Property(entry => entry.GemSize)
                            .IsRequired();

                     builder.Property(entry => entry.VocationRestriction);

                     builder.Property(entry => entry.Description)
                            .HasMaxLength(2000);

                     builder.Property(entry => entry.LastUpdated)
                            .HasProviderDateTimeColumnType();

                     builder.HasIndex(entry => entry.Name)
                            .IsUnique();

                     builder.HasIndex(entry => new
                     {
                            entry.GemFamily,
                            entry.GemSize
                     });

                     builder.HasIndex(entry => entry.VocationRestriction);
              }
       }
}
