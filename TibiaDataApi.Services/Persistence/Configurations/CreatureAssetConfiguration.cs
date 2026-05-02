using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Assets;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class CreatureAssetConfiguration : IEntityTypeConfiguration<CreatureAsset>
    {
        public void Configure(EntityTypeBuilder<CreatureAsset> builder)
        {
            builder.ToTable("creature_assets");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new
            {
                x.CreatureId,
                x.AssetKind,
                x.SortOrder
            });
            builder.HasIndex(x => new
            {
                x.CreatureId,
                x.AssetId
            }).IsUnique();

            builder.HasOne(x => x.Creature)
                   .WithMany(x => x.CreatureAssets)
                   .HasForeignKey(x => x.CreatureId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Asset)
                   .WithMany(x => x.CreatureAssets)
                   .HasForeignKey(x => x.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}