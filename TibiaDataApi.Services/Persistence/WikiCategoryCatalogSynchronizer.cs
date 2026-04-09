using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.Entities.Categories;

namespace TibiaDataApi.Services.Persistence
{
    public sealed class WikiCategoryCatalogSynchronizer
    {
        public async Task SynchronizeAsync(
            TibiaDbContext dbContext,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, WikiCategory> existingCategories = await dbContext.WikiCategories
                                                                                 .ToDictionaryAsync(entry => entry.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach(WikiCategoryDefinition definition in TibiaWikiCategoryCatalog.All)
            {
                if(existingCategories.TryGetValue(definition.Slug, out WikiCategory? existingCategory))
                {
                    existingCategory.Name = definition.Name;
                    existingCategory.ContentType = definition.ContentType;
                    existingCategory.GroupSlug = definition.GroupSlug;
                    existingCategory.GroupName = definition.GroupName;
                    existingCategory.SourceKind = definition.SourceKind;
                    existingCategory.SourceTitle = definition.SourceTitle;
                    existingCategory.SourceSection = definition.SourceSection;
                    existingCategory.ObjectClass = definition.ObjectClass;
                    existingCategory.SortOrder = definition.SortOrder;
                    existingCategory.IsActive = definition.IsActive;
                    existingCategory.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                dbContext.WikiCategories.Add(new WikiCategory
                {
                    Slug = definition.Slug,
                    Name = definition.Name,
                    ContentType = definition.ContentType,
                    GroupSlug = definition.GroupSlug,
                    GroupName = definition.GroupName,
                    SourceKind = definition.SourceKind,
                    SourceTitle = definition.SourceTitle,
                    SourceSection = definition.SourceSection,
                    ObjectClass = definition.ObjectClass,
                    SortOrder = definition.SortOrder,
                    IsActive = definition.IsActive
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Wiki category catalog synchronized. {Count} categories are available.",
                TibiaWikiCategoryCatalog.All.Count);
        }
    }
}