using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Categories;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.DataBaseService.Categories.Interfaces;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.DataBaseService.Categories
{
    public sealed class CategoriesDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : ICategoriesDataBaseService
    {
        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        private readonly TibiaDbContext _db = db;
        private readonly HybridCache _hybridCache = hybridCache;

        public async Task<IReadOnlyList<CategoryListItemResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await _hybridCache.GetOrCreateAsync(
                "categories:list",
                async ct => await _db.WikiCategories
                                     .AsNoTracking()
                                     .Where(x => x.IsActive)
                                     .OrderBy(x => x.SortOrder)
                                     .ThenBy(x => x.Name)
                                     .Select(x => new CategoryListItemResponse(
                                         x.Id,
                                         x.Slug,
                                         x.Name,
                                         x.ContentType.ToString(),
                                         x.GroupSlug,
                                         x.GroupName,
                                         x.ObjectClass,
                                         x.SortOrder))
                                     .ToListAsync(ct),
                _cacheOptions,
                [CacheTags.Categories],
                cancellationToken);
        }

        public async Task<CategoryDetailsResponse?> GetCategoryDetailsBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            string normalizedSlug = slug.Trim().ToLowerInvariant();

            return await _hybridCache.GetOrCreateAsync(
                $"categories:details:{normalizedSlug}",
                async ct =>
                {
                    WikiCategory? category = await _db.WikiCategories
                                                      .AsNoTracking()
                                                      .FirstOrDefaultAsync(x => x.Slug == normalizedSlug && x.IsActive, ct);

                    if(category is null)
                    {
                        return null;
                    }

                    int itemCount = await _db.Items
                                             .AsNoTracking()
                                             .CountAsync(x => x.CategoryId == category.Id && !x.IsMissingFromSource, ct);

                    int wikiArticleCount = await _db.WikiArticleCategories
                                                    .AsNoTracking()
                                                    .CountAsync(x => x.WikiCategoryId == category.Id
                                                                     && !x.IsMissingFromSource
                                                                     && x.WikiArticle != null
                                                                     && !x.WikiArticle.IsMissingFromSource,
                                                        ct);

                    return MapCategoryDetails(category, itemCount, wikiArticleCount);
                },
                _cacheOptions,
                [CacheTags.Categories],
                cancellationToken);
        }

        public async Task<CategoryDetailsResponse?> GetCategoryDetailsByIdAsync(int categoryId, CancellationToken cancellationToken = default)
        {
            return await _hybridCache.GetOrCreateAsync(
                $"categories:details:{categoryId}",
                async ct =>
                {
                    WikiCategory? category = await _db.WikiCategories
                                                      .AsNoTracking()
                                                      .FirstOrDefaultAsync(x => x.Id == categoryId && x.IsActive, ct);

                    if(category is null)
                    {
                        return null;
                    }

                    int itemCount = await _db.Items
                                             .AsNoTracking()
                                             .CountAsync(x => x.CategoryId == category.Id && !x.IsMissingFromSource, ct);

                    int wikiArticleCount = await _db.WikiArticleCategories
                                                    .AsNoTracking()
                                                    .CountAsync(x => x.WikiCategoryId == category.Id
                                                                     && !x.IsMissingFromSource
                                                                     && x.WikiArticle != null
                                                                     && !x.WikiArticle.IsMissingFromSource,
                                                        ct);

                    return MapCategoryDetails(category, itemCount, wikiArticleCount);
                },
                _cacheOptions,
                [CacheTags.Categories],
                cancellationToken);
        }

        private static CategoryListItemResponse MapCategoryListItem(WikiCategory category)
        {
            return new CategoryListItemResponse(
                category.Id,
                category.Slug,
                category.Name,
                category.ContentType.ToString(),
                category.GroupSlug,
                category.GroupName,
                category.ObjectClass,
                category.SortOrder);
        }

        private static CategoryDetailsResponse MapCategoryDetails(WikiCategory category, int itemCount, int wikiArticleCount)
        {
            return new CategoryDetailsResponse(
                category.Id,
                category.Slug,
                category.Name,
                category.ContentType.ToString(),
                category.GroupSlug,
                category.GroupName,
                category.SourceKind.ToString(),
                category.SourceTitle,
                category.SourceSection,
                category.ObjectClass,
                category.SortOrder,
                category.IsActive,
                category.CreatedAt,
                category.UpdatedAt,
                itemCount,
                wikiArticleCount);
        }
    }
}