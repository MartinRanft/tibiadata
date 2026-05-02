using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Creatures;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public class CreatureConfiguration : IEntityTypeConfiguration<Creature>
    {
        public void Configure(EntityTypeBuilder<Creature> builder)
        {
            builder.ToTable("creatures");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.NormalizedName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.InfoboxJson)
                   .HasProviderJsonColumnType();

            builder.Property(x => x.BestiaryJson)
                   .HasProviderJsonColumnType();

            builder.Property(x => x.LootStatisticsJson)
                   .HasProviderJsonColumnType();

            builder.HasIndex(x => x.Name).IsUnique();
            builder.HasIndex(x => x.NormalizedName).IsUnique();
        }
    }
}
