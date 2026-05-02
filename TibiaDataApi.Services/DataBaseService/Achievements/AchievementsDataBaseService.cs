using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Achievements;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Achievements.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Achievements
{
    public sealed class AchievementsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IAchievementsDataBaseService
    {
        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<List<AchievementListItemResponse>> GetAchievementsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "achievements:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Achievement)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article =>
                                   {
                                       IReadOnlyDictionary<string, string>? infobox = StructuredJsonParser.ParseStringDictionary(article.InfoboxJson);

                                       if(infobox is null || !infobox.ContainsKey("achievementid"))
                                       {
                                           return null;
                                       }

                                       string name = GetInfoboxValue(infobox, "name")
                                                     ?? article.Title;

                                       string? summary = string.IsNullOrWhiteSpace(article.Summary)
                                       ? GetInfoboxValue(infobox, "description")
                                       : article.Summary;

                                       return new AchievementListItemResponse(
                                           article.Id,
                                           name,
                                           summary,
                                           article.WikiUrl,
                                           article.LastUpdated);
                                   })
                                   .Where(response => response is not null)
                                   .Select(response => response!)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Achievements],
                cancellationToken);
        }

        public async Task<AchievementDetailsResponse?> GetAchievementDetailsByNameAsync(
            string achievementName,
            CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(achievementName);

            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            CachedAchievementDetailsResult result = await hybridCache.GetOrCreateAsync(
                $"achievements:by-name:{normalizedName}",
                async ct =>
                {
                    int articleId = await db.WikiArticles
                                            .AsNoTracking()
                                            .Where(x => x.ContentType == WikiContentType.Achievement)
                                            .Where(x => !x.IsMissingFromSource)
                                            .Where(x => x.NormalizedTitle == normalizedName)
                                            .Select(x => x.Id)
                                            .SingleOrDefaultAsync(ct);

                    if(articleId <= 0)
                    {
                        return CachedAchievementDetailsResult.NotFound;
                    }

                    AchievementDetailsResponse? details = await GetAchievementDetailsByIdAsync(articleId, ct);
                    return details is null
                    ? CachedAchievementDetailsResult.NotFound
                    : new CachedAchievementDetailsResult(true, details);
                },
                _cacheOptions,
                [CacheTags.Achievements],
                cancellationToken);

            return result.Found ? result.Achievement : null;
        }

        public async Task<AchievementDetailsResponse?> GetAchievementDetailsByIdAsync(
            int achievementId,
            CancellationToken cancellationToken = default)
        {
            if(achievementId <= 0)
            {
                return null;
            }

            CachedAchievementDetailsResult result = await hybridCache.GetOrCreateAsync(
                $"achievements:by-id:{achievementId}",
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Achievement)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == achievementId)
                                                   .SingleOrDefaultAsync(ct);

                    if(article is null)
                    {
                        return CachedAchievementDetailsResult.NotFound;
                    }

                    IReadOnlyDictionary<string, string>? infobox = StructuredJsonParser.ParseStringDictionary(article.InfoboxJson);
                    if(infobox is null || !infobox.ContainsKey("achievementid"))
                    {
                        return CachedAchievementDetailsResult.NotFound;
                    }

                    string name = GetInfoboxValue(infobox, "name")
                                  ?? article.Title;

                    AchievementStructuredDataResponse structuredData = new(
                        NormalizeTemplate(article.InfoboxTemplate),
                        MapAchievementInfobox(infobox));

                    AchievementDetailsResponse response = new(
                        article.Id,
                        name,
                        GetInfoboxValue(infobox, "actualname"),
                        GetInfoboxValue(infobox, "description"),
                        GetInfoboxValue(infobox, "spoiler"),
                        GetInfoboxValue(infobox, "grade"),
                        GetInfoboxValue(infobox, "points"),
                        GetInfoboxValue(infobox, "premium"),
                        GetInfoboxValue(infobox, "secret"),
                        GetInfoboxValue(infobox, "implemented"),
                        GetInfoboxValue(infobox, "achievementid"),
                        GetInfoboxValue(infobox, "relatedpages"),
                        GetInfoboxValue(infobox, "history"),
                        GetInfoboxValue(infobox, "status"),
                        article.PlainTextContent,
                        article.RawWikiText,
                        structuredData,
                        article.WikiUrl,
                        article.LastSeenAt,
                        article.LastUpdated);

                    return new CachedAchievementDetailsResult(true, response);
                },
                _cacheOptions,
                [CacheTags.Achievements],
                cancellationToken);

            return result.Found ? result.Achievement : null;
        }

        private static string? GetInfoboxValue(
            IReadOnlyDictionary<string, string> infobox,
            string key)
        {
            if(!infobox.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value;
        }

        private static AchievementInfoboxResponse? MapAchievementInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new AchievementInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "actualname"),
                GetInfoboxValue(infobox, "description"),
                GetInfoboxValue(infobox, "spoiler"),
                GetInfoboxValue(infobox, "grade"),
                GetInfoboxValue(infobox, "points"),
                GetInfoboxValue(infobox, "premium"),
                GetInfoboxValue(infobox, "secret"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "achievementid"),
                GetInfoboxValue(infobox, "relatedpages"),
                GetInfoboxValue(infobox, "history"),
                GetInfoboxValue(infobox, "status"),
                GetInfoboxValue(infobox, "coincideswith"),
                infobox);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private sealed record CachedAchievementDetailsResult(
            bool Found,
            AchievementDetailsResponse? Achievement)
        {
            public static readonly CachedAchievementDetailsResult NotFound = new(false, null);
        }
    }
}
