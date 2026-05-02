using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class WheelPerkConfiguration : IEntityTypeConfiguration<WheelPerk>
    {
        public void Configure(EntityTypeBuilder<WheelPerk> builder)
        {
            builder.ToTable("wheel_perks");

            builder.HasKey(entry => entry.Id);

            builder.Property(entry => entry.Key)
                   .IsRequired()
                   .HasMaxLength(191);

            builder.Property(entry => entry.Slug)
                   .IsRequired()
                   .HasMaxLength(128);

            builder.Property(entry => entry.Name)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(entry => entry.Summary)
                   .HasMaxLength(2000);

            builder.Property(entry => entry.Description)
                   .HasProviderLargeTextColumnType();

            builder.Property(entry => entry.MainSourceTitle)
                   .HasMaxLength(255);

            builder.Property(entry => entry.MainSourceUrl)
                   .HasMaxLength(500);

            builder.Property(entry => entry.MetadataJson)
                   .HasProviderJsonColumnType();

            builder.Property(entry => entry.LastUpdated)
                   .HasProviderDateTimeColumnType();

            builder.HasIndex(entry => entry.Key)
                   .IsUnique();

            builder.HasIndex(entry => new
                   {
                       entry.Vocation,
                       entry.Slug
                   })
                   .IsUnique();

            builder.HasIndex(entry => new
                   {
                       entry.Vocation,
                       entry.Type
                   });

            builder.HasIndex(entry => new
                   {
                       entry.Vocation,
                       entry.IsActive
                   });

            builder.Navigation(entry => entry.Occurrences)
                   .AutoInclude(false);

            builder.Navigation(entry => entry.Stages)
                   .AutoInclude(false);
        }
    }
}
