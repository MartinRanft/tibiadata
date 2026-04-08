using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Content;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class WikiArticleCategoryConfiguration : IEntityTypeConfiguration<WikiArticleCategory>
    {
        public void Configure(EntityTypeBuilder<WikiArticleCategory> builder)
        {
            builder.ToTable("wiki_article_categories");

            builder.HasKey(x => new
            {
                x.WikiArticleId,
                x.WikiCategoryId
            });

            builder.HasOne(x => x.WikiArticle)
                   .WithMany(x => x.WikiArticleCategories)
                   .HasForeignKey(x => x.WikiArticleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.WikiCategory)
                   .WithMany(x => x.WikiArticleCategories)
                   .HasForeignKey(x => x.WikiCategoryId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.WikiCategoryId);
            builder.HasIndex(x => x.LastSeenAt);
            builder.HasIndex(x => x.IsMissingFromSource);
        }
    }
}