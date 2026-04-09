using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Quests;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Quests.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Quests
{
    public sealed class QuestsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IQuestsDataBaseService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<IReadOnlyList<QuestListItemResponse>> GetQuestsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "quests:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Quest)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseQuestInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsQuestDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapQuestListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Quests],
                cancellationToken);
        }

        public async Task<QuestDetailsResponse?> GetQuestDetailsByNameAsync(string questName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(questName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"quests:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int questId = await db.WikiArticles
                                          .AsNoTracking()
                                          .Where(x => x.ContentType == WikiContentType.Quest)
                                          .Where(x => !x.IsMissingFromSource)
                                          .Where(x => x.NormalizedTitle == normalizedName)
                                          .Select(x => x.Id)
                                          .FirstOrDefaultAsync(ct);

                    if(questId <= 0)
                    {
                        return null;
                    }

                    return await GetQuestDetailsByIdAsync(questId, ct);
                },
                _cacheOptions,
                [CacheTags.Quests],
                cancellationToken);
        }

        public async Task<QuestDetailsResponse?> GetQuestDetailsByIdAsync(int questId, CancellationToken cancellationToken = default)
        {
            if(questId <= 0)
            {
                return null;
            }

            string cacheKey = ($"quests:by-id:{questId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Quest)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == questId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseQuestInfobox(article.InfoboxJson);
                    if(!IsQuestDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapQuestDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Quests],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetQuestSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "quests:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Quest)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseQuestInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsQuestDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Quests],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetQuestSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"quests:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Quest)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseQuestInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsQuestDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Quests],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseQuestInfobox(string? infoboxJson)
        {
            if(string.IsNullOrWhiteSpace(infoboxJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(infoboxJson, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool IsQuestDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            return string.Equals(normalizedTemplate, "Infobox Quest", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static QuestListItemResponse MapQuestListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new QuestListItemResponse(
                article.Id,
                GetQuestName(article, infobox),
                BuildQuestSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static QuestDetailsResponse MapQuestDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new QuestDetailsResponse(
                article.Id,
                GetQuestName(article, infobox),
                BuildQuestSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static QuestStructuredDataResponse? CreateStructuredData(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) && (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new QuestStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapQuestInfobox(infobox));
        }

        private static QuestInfoboxResponse? MapQuestInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new QuestInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "aka"),
                GetInfoboxValue(infobox, "type"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "premium"),
                GetInfoboxValue(infobox, "level"),
                GetInfoboxValue(infobox, "levelrecommended"),
                GetInfoboxValue(infobox, "levelnote"),
                GetInfoboxValue(infobox, "location"),
                GetInfoboxValue(infobox, "dangers"),
                GetInfoboxValue(infobox, "legend"),
                GetInfoboxValue(infobox, "reward"),
                GetInfoboxValue(infobox, "log"),
                GetInfoboxValue(infobox, "time"),
                GetInfoboxValue(infobox, "timeallocation"),
                GetInfoboxValue(infobox, "transcripts"),
                GetInfoboxValue(infobox, "rookgaardquest"),
                GetHistory(infobox),
                GetStatus(infobox),
                infobox);
        }

        private static string GetQuestName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name") ?? article.Title;
        }

        private static string? BuildQuestSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? summary = NormalizeQuestText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            List<string> parts = [];
            string? type = GetInfoboxValue(infobox, "type");
            string? location = GetInfoboxValue(infobox, "location");
            string? level = GetInfoboxValue(infobox, "levelrecommended") ?? GetInfoboxValue(infobox, "level");
            string? reward = GetInfoboxValue(infobox, "reward");

            if(!string.IsNullOrWhiteSpace(type))
            {
                parts.Add($"{type}.");
            }

            if(!string.IsNullOrWhiteSpace(location))
            {
                parts.Add($"Location: {location}.");
            }

            if(!string.IsNullOrWhiteSpace(level))
            {
                parts.Add($"Level: {level}.");
            }
            else if(!string.IsNullOrWhiteSpace(reward))
            {
                parts.Add($"Reward: {reward}.");
            }

            if(parts.Count == 0)
            {
                return null;
            }

            string combined = string.Join(" ", parts).Trim();
            return combined.Length <= 280 ? combined : combined[..277].TrimEnd() + "...";
        }

        private static string? GetHistory(IReadOnlyDictionary<string, string>? infobox)
        {
            return NormalizeQuestText(GetInfoboxValue(infobox, "history"));
        }

        private static string? GetStatus(IReadOnlyDictionary<string, string>? infobox)
        {
            return NormalizeQuestText(GetInfoboxValue(infobox, "status"));
        }

        private static string? GetInfoboxValue(IReadOnlyDictionary<string, string>? infobox, string key)
        {
            if(infobox is null || !infobox.TryGetValue(key, out string? value))
            {
                return null;
            }

            return NormalizeQuestText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeQuestText(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Replace("\r", " ")
                                     .Replace("\n", " ")
                                     .Replace("\\n", " ")
                                     .Replace("<br />", " ", StringComparison.OrdinalIgnoreCase)
                                     .Replace("<br/>", " ", StringComparison.OrdinalIgnoreCase)
                                     .Replace("<br>", " ", StringComparison.OrdinalIgnoreCase)
                                     .Trim();

            while (normalized.Contains("  ", StringComparison.Ordinal))
            {
                normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
            }

            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }
    }
}
