using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaDataApi.Services.Entities.Content;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class WikiArticleConfiguration : IEntityTypeConfiguration<WikiArticle>
    {
        public void Configure(EntityTypeBuilder<WikiArticle> builder)
        {
            builder.ToTable("wiki_articles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ContentType)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(x => x.Title)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.NormalizedTitle)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.Summary)
                   .HasMaxLength(8000);

            builder.Property(x => x.RawWikiText)
                   .HasProviderLargeTextColumnType();

            builder.Property(x => x.InfoboxTemplate)
                   .HasProviderLargeTextColumnType();

            builder.Property(x => x.InfoboxJson)
                   .HasProviderJsonColumnType();

            builder.Property(x => x.AdditionalAttributesJson)
                   .HasProviderJsonColumnType();

            builder.Property(x => x.WikiUrl)
                   .HasMaxLength(500);

            ValueConverter<List<string>, string> listConverter = new(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>());

            ValueComparer<List<string>> listComparer = new(
                (left, right) => SequenceEqualSafe(left, right),
                value => GetListHashCode(value),
                value => Snapshot(value));

            builder.Property(x => x.Sections)
                   .HasConversion(listConverter, listComparer)
                   .HasProviderJsonColumnType();

            builder.Property(x => x.LinkedTitles)
                   .HasConversion(listConverter, listComparer)
                   .HasProviderJsonColumnType();

            builder.HasIndex(x => new
                   {
                       x.ContentType,
                       x.NormalizedTitle
                   })
                   .IsUnique();
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

            return values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode()));
        }

        private static List<string> Snapshot(List<string>? source)
        {
            return source?.ToList() ?? [];
        }
    }
}
