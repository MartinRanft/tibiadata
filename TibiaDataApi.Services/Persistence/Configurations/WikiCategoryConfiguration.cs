using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Categories;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public class WikiCategoryConfiguration : IEntityTypeConfiguration<WikiCategory>
    {
        public void Configure(EntityTypeBuilder<WikiCategory> builder)
        {
            builder.ToTable("wiki_categories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Slug)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.ContentType)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(x => x.GroupSlug)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.GroupName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.SourceKind)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(x => x.SourceTitle)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.SourceSection)
                   .HasMaxLength(200);

            builder.Property(x => x.ObjectClass)
                   .HasMaxLength(150);

            builder.HasIndex(x => x.Slug).IsUnique();
            builder.HasIndex(x => new
            {
                x.ContentType,
                x.GroupSlug
            });
            builder.HasIndex(x => new
            {
                x.ContentType,
                x.SortOrder
            });
        }
    }
}