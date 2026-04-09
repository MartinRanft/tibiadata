using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.HuntingPlaces;
using TibiaDataApi.Contracts.Public.WikiArticles;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.HuntingPlaces.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.HuntingPlaces
{
    public sealed class HuntingPlacesDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IHuntingPlacesDataBaseService
    {
        private const string HuntingPlacesCategorySlug = "hunting-places";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<List<HuntingPlaceListItemResponse>> GetHuntingPlacesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "hunting-places-list",
                async cancel =>
                {
                    List<HuntingPlaceListReadModel> articles = await db.WikiArticles
                                                                       .AsNoTracking()
                                                                       .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                                                       .Where(x => !x.IsMissingFromSource)
                                                                       .OrderBy(x => x.Title)
                                                                       .Select(x => new HuntingPlaceListReadModel(
                                                                           x.Id,
                                                                           x.Title,
                                                                           x.Summary,
                                                                           x.InfoboxJson,
                                                                           x.WikiUrl,
                                                                           x.LastUpdated))
                                                                       .ToListAsync(cancel);

                    return articles.Select(MapListItem)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken);
        }

        public async Task<HuntingPlaceDetailsResponse?> GetHuntingPlaceDetailsByNameAsync(
            string huntingPlaceName,
            CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(huntingPlaceName);

            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"hunting-place-details-by-name:{normalizedName}",
                async cancel =>
                {
                    int articleId = await db.WikiArticles
                                            .AsNoTracking()
                                            .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                            .Where(x => !x.IsMissingFromSource)
                                            .Where(x => x.NormalizedTitle == normalizedName)
                                            .Select(x => x.Id)
                                            .SingleOrDefaultAsync(cancel);

                    return articleId <= 0 ? null : await GetHuntingPlaceDetailsByIdAsync(articleId, cancel);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken);
        }

        public async Task<HuntingPlaceDetailsResponse?> GetHuntingPlaceDetailsByIdAsync(
            int huntingPlaceId,
            CancellationToken cancellationToken = default)
        {
            if(huntingPlaceId <= 0)
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"hunting-place-details:{huntingPlaceId}",
                async cancel =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                                   .Where(x => x.Id == huntingPlaceId)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .FirstOrDefaultAsync(cancel);

                    if(article is null)
                    {
                        return null;
                    }

                    List<WikiArticleCategoryResponse> articleCategories = await db.WikiArticleCategories
                                                                                  .AsNoTracking()
                                                                                  .Where(c => c.WikiArticleId == article.Id)
                                                                                  .Where(c => c.WikiCategory != null && !c.IsMissingFromSource)
                                                                                  .Select(c => new WikiArticleCategoryResponse(
                                                                                      c.WikiCategoryId,
                                                                                      c.WikiCategory!.Slug,
                                                                                      c.WikiCategory.Name,
                                                                                      c.WikiCategory.GroupSlug,
                                                                                      c.WikiCategory.GroupName))
                                                                                  .ToListAsync(cancel);

                    return MapDetails(article, articleCategories);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken);
        }

        public async Task<HuntingPlaceAreaRecommendationResponse?> GetHuntingPlaceAreaRecommendationAsync(
            string huntingPlaceName,
            CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(huntingPlaceName);

            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"hunting-place-area-recommendation-by-name:{normalizedName}",
                async cancel =>
                {
                    HuntingPlaceAreaReadModel? article = await db.WikiArticles
                                                                 .AsNoTracking()
                                                                 .Where(x => x.NormalizedTitle == normalizedName)
                                                                 .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                                                 .Where(x => !x.IsMissingFromSource)
                                                                 .OrderBy(x => x.Id)
                                                                 .Select(x => new HuntingPlaceAreaReadModel(
                                                                     x.InfoboxJson,
                                                                     x.AdditionalAttributesJson))
                                                                 .FirstOrDefaultAsync(cancel);

                    if(article is null)
                    {
                        return null;
                    }

                    HuntingPlaceAdditionalAttributes? additionalAttributes = DeserializeAdditionalAttributes(article.AdditionalAttributesJson);
                    List<HuntingPlaceAreaRecommendationResponse> lowerLevels = BuildLowerLevels(additionalAttributes);
                    if(lowerLevels.Count > 0)
                    {
                        return lowerLevels[0];
                    }

                    HuntingPlaceInfobox? infobox = DeserializeInfobox(article.InfoboxJson);
                    return infobox is null ? null : CreateAreaRecommendation(infobox);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetHuntingPlaceUpdates(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "hunting-places:sync-states",
                async cancel =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                   .OrderBy(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       x.LastSeenAt))
                                   .ToListAsync(cancel);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken
            );
        }
        public async Task<List<SyncStateResponse>> GetHuntingPlaceUpdatesByDate(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "hunting-places:sync-states-byDate",
                async cancel =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.HuntingPlace)
                                   .Where(x => x.LastUpdated >= time)
                                   .OrderBy(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       x.LastSeenAt))
                                   .ToListAsync(cancel);
                },
                _cacheOptions,
                [CacheTags.HuntingPlaces, CacheTags.Categories, CacheTags.Category(HuntingPlacesCategorySlug)],
                cancellationToken
            );
        }

        private static HuntingPlaceDetailsResponse MapDetails(
            WikiArticle article,
            IReadOnlyList<WikiArticleCategoryResponse> articleCategories)
        {
            HuntingPlaceInfobox? infobox = DeserializeInfobox(article.InfoboxJson);
            HuntingPlaceAdditionalAttributes? additionalAttributes = DeserializeAdditionalAttributes(article.AdditionalAttributesJson);
            List<HuntingPlaceAreaRecommendationResponse> lowerLevels = BuildLowerLevels(additionalAttributes);
            HuntingPlaceStructuredDataResponse structuredData = new(
                article.InfoboxTemplate,
                infobox,
                additionalAttributes);

            return new HuntingPlaceDetailsResponse(
                article.Id,
                article.Title,
                article.Summary,
                article.PlainTextContent,
                article.RawWikiText,
                structuredData,
                infobox?.Image,
                infobox?.City,
                infobox?.Location,
                infobox?.Vocation,
                infobox?.LevelKnights,
                infobox?.LevelPaladins,
                infobox?.LevelMages,
                infobox?.SkillKnights,
                infobox?.SkillPaladins,
                infobox?.SkillMages,
                infobox?.DefenseKnights,
                infobox?.DefensePaladins,
                infobox?.DefenseMages,
                infobox?.Loot,
                infobox?.LootStar,
                infobox?.Experience,
                infobox?.ExperienceStar,
                infobox?.BestLoot,
                infobox?.BestLoot2,
                infobox?.BestLoot3,
                infobox?.BestLoot4,
                infobox?.BestLoot5,
                infobox?.Map,
                infobox?.Map2,
                infobox?.Map3,
                infobox?.Map4,
                lowerLevels,
                articleCategories,
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static HuntingPlaceListItemResponse MapListItem(HuntingPlaceListReadModel article)
        {
            HuntingPlaceInfobox? infobox = DeserializeInfobox(article.InfoboxJson);

            return new HuntingPlaceListItemResponse(
                article.Id,
                article.Title,
                article.Summary,
                infobox?.City,
                infobox?.Location,
                infobox?.Vocation,
                article.WikiUrl,
                article.LastUpdated);
        }

        private static HuntingPlaceInfobox? DeserializeInfobox(string? infoboxJson)
        {
            if(string.IsNullOrWhiteSpace(infoboxJson))
            {
                return null;
            }

            try
            {
                HuntingPlaceInfobox? infobox = JsonSerializer.Deserialize<HuntingPlaceInfobox>(infoboxJson, JsonOptions);

                if(infobox is not null)
                {
                    infobox.Fields = StructuredJsonParser.ParseStringDictionary(infoboxJson);
                }

                return infobox;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static HuntingPlaceAdditionalAttributes? DeserializeAdditionalAttributes(string? additionalAttributesJson)
        {
            if(string.IsNullOrWhiteSpace(additionalAttributesJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<HuntingPlaceAdditionalAttributes>(additionalAttributesJson, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static List<HuntingPlaceAreaRecommendationResponse> BuildLowerLevels(HuntingPlaceAdditionalAttributes? additionalAttributes)
        {
            return additionalAttributes?.LowerLevels?
                                       .Select(CreateAreaRecommendation)
                                       .ToList()
                   ?? [];
        }

        private static HuntingPlaceAreaRecommendationResponse CreateAreaRecommendation(HuntingPlaceLowerLevel lowerLevel)
        {
            return new HuntingPlaceAreaRecommendationResponse(
                lowerLevel.AreaName,
                lowerLevel.LevelKnights,
                lowerLevel.LevelPaladins,
                lowerLevel.LevelMages,
                lowerLevel.SkillKnights,
                lowerLevel.SkillPaladins,
                lowerLevel.SkillMages,
                lowerLevel.DefenseKnights,
                lowerLevel.DefensePaladins,
                lowerLevel.DefenseMages);
        }

        private static HuntingPlaceAreaRecommendationResponse CreateAreaRecommendation(HuntingPlaceInfobox infobox)
        {
            return new HuntingPlaceAreaRecommendationResponse(
                infobox.AreaName,
                infobox.LevelKnights,
                infobox.LevelPaladins,
                infobox.LevelMages,
                infobox.SkillKnights,
                infobox.SkillPaladins,
                infobox.SkillMages,
                infobox.DefenseKnights,
                infobox.DefensePaladins,
                infobox.DefenseMages);
        }

        private sealed record HuntingPlaceListReadModel(
            int Id,
            string Title,
            string? Summary,
            string? InfoboxJson,
            string? WikiUrl,
            DateTime LastUpdated);

        private sealed record HuntingPlaceAreaReadModel(
            string? InfoboxJson,
            string? AdditionalAttributesJson);
    }
}
