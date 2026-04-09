using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaDataApi.Services.Entities.Items;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public class ItemConfiguration : IEntityTypeConfiguration<Item>
    {
        public void Configure(EntityTypeBuilder<Item> builder)
        {
            builder.ToTable("items");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Name).IsUnique();
            builder.HasIndex(x => x.NormalizedName).IsUnique();
            builder.HasIndex(x => x.NormalizedActualName);
            builder.HasIndex(x => x.CategoryId);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.NormalizedName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.NormalizedActualName)
                   .HasMaxLength(255);

            
            ValueConverter<List<string>, string> listConverter = new(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

            
            ValueComparer<List<string>> listComparer = new(
                (c1, c2) => SequenceEqualSafe(c1, c2),
                c => GetListHashCode(c),
                c => Snapshot(c));

            
            builder.Property(x => x.ItemId).HasConversion(listConverter, listComparer).HasProviderJsonColumnType();
            builder.Property(x => x.DroppedBy).HasConversion(listConverter, listComparer).HasProviderJsonColumnType();
            builder.Property(x => x.Sounds).HasConversion(listConverter, listComparer).HasProviderJsonColumnType();
            builder.Property(x => x.AdditionalAttributesJson).HasProviderJsonColumnType();

            builder.HasOne(x => x.Category)
                   .WithMany(x => x.Items)
                   .HasForeignKey(x => x.CategoryId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.Navigation(x => x.ItemAssets)
                   .AutoInclude(false);
        }

        private static bool SequenceEqualSafe(List<string>? first, List<string>? second)
        {
            IEnumerable<string> left = first ?? Enumerable.Empty<string>();
            IEnumerable<string> right = second ?? Enumerable.Empty<string>();

            return left.SequenceEqual(right);
        }

        private static int GetListHashCode(List<string>? source)
        {
            IEnumerable<string> values = source ?? Enumerable.Empty<string>();

            return values
            .Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode()));
        }

        private static List<string> Snapshot(List<string>? source)
        {
            return source?.ToList() ?? new List<string>();
        }
    }
}
