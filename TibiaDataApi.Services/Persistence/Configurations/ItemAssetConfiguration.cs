using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Assets;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public class ItemAssetConfiguration : IEntityTypeConfiguration<ItemAsset>
    {
        public void Configure(EntityTypeBuilder<ItemAsset> builder)
        {
            builder.ToTable("item_assets");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new
            {
                x.ItemId,
                x.AssetKind,
                x.SortOrder
            });
            builder.HasIndex(x => new
            {
                x.ItemId,
                x.AssetId
            }).IsUnique();

            builder.HasOne(x => x.Item)
                   .WithMany(x => x.ItemAssets)
                   .HasForeignKey(x => x.ItemId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Asset)
                   .WithMany(x => x.ItemAssets)
                   .HasForeignKey(x => x.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}