using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Charms;
using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Charms.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Charms
{
    public sealed class CharmsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : ICharmsDataBaseService
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

        public async Task<IReadOnlyList<CharmListItemResponse>> GetCharmsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "charms:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Charm)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseCharmInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsCharmDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapCharmListItem(x.Article, x.Infobox))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Charms],
                cancellationToken);
        }

        public async Task<CharmDetailsResponse?> GetCharmDetailsByNameAsync(string charmName, CancellationToken cancellationToken = default)
        {
            string normalizedCharmName = EntityNameNormalizer.Normalize(charmName);
            if(string.IsNullOrWhiteSpace(normalizedCharmName))
            {
                return null;
            }

            string cacheKey = ($"charms:by-name:{normalizedCharmName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int charmId = await db.WikiArticles
                                          .AsNoTracking()
                                          .Where(x => x.ContentType == WikiContentType.Charm)
                                          .Where(x => !x.IsMissingFromSource)
                                          .Where(x => x.NormalizedTitle == normalizedCharmName)
                                          .Select(x => x.Id)
                                          .FirstOrDefaultAsync(ct);

                    if(charmId <= 0)
                    {
                        return null;
                    }

                    return await GetCharmDetailsByIdAsync(charmId, ct);
                },
                _cacheOptions,
                [CacheTags.Charms],
                cancellationToken);
        }

        public async Task<CharmDetailsResponse?> GetCharmDetailsByIdAsync(int charmId, CancellationToken cancellationToken = default)
        {
            if(charmId <= 0)
            {
                return null;
            }

            string cacheKey = ($"charms:by-id:{charmId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Charm)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == charmId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseCharmInfobox(article.InfoboxJson);
                    if(!IsCharmDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapCharmDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Charms],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetCharmSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "charms:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Charm)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseCharmInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsCharmDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(
                                       x.Article.Id,
                                       x.Article.LastUpdated,
                                       x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Charms],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetCharmSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"charms:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Charm)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseCharmInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsCharmDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(
                                       x.Article.Id,
                                       x.Article.LastUpdated,
                                       x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Charms],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseCharmInfobox(string? infoboxJson)
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

        private static bool IsCharmDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            if(!string.Equals(normalizedTemplate, "Infobox Charm", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static CharmListItemResponse MapCharmListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new CharmListItemResponse(
                article.Id,
                GetCharmName(article, infobox),
                GetCharmActualName(infobox),
                GetCharmType(infobox),
                GetCharmCost(infobox),
                GetCharmEffect(infobox),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static CharmDetailsResponse MapCharmDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new CharmDetailsResponse(
                article.Id,
                GetCharmName(article, infobox),
                GetCharmActualName(infobox),
                GetCharmType(infobox),
                GetCharmCost(infobox),
                GetCharmEffect(infobox),
                GetCharmImplemented(infobox),
                GetCharmNotes(infobox),
                GetCharmHistory(infobox),
                GetCharmStatus(infobox),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static CharmStructuredDataResponse? CreateStructuredData(
            WikiArticle article,
            IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) &&
               (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new CharmStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapCharmInfobox(infobox));
        }

        private static CharmInfoboxResponse? MapCharmInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new CharmInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetCharmActualName(infobox),
                GetInfoboxValue(infobox, "type"),
                GetInfoboxValue(infobox, "cost"),
                GetInfoboxValue(infobox, "effect"),
                GetInfoboxValue(infobox, "implemented"),
                GetCharmNotes(infobox),
                GetCharmHistory(infobox),
                GetCharmStatus(infobox));
        }

        private static string GetCharmName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name")
                   ?? article.Title;
        }

        private static string? GetCharmActualName(IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "actualname");
        }

        private static string GetCharmType(IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "type")
                   ?? "Unknown";
        }

        private static string GetCharmCost(IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "cost")
                   ?? string.Empty;
        }

        private static string GetCharmEffect(IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "effect")
                   ?? string.Empty;
        }

        private static string GetCharmImplemented(IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "implemented")
                   ?? string.Empty;
        }

        private static string? GetCharmNotes(IReadOnlyDictionary<string, string>? infobox)
        {
            string? notes = NormalizeCharmText(GetInfoboxValue(infobox, "notes"));
            if(string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            if(ExtractPrefixedValue(notes, "history") is not null ||
               ExtractPrefixedValue(notes, "status") is not null)
            {
                return null;
            }

            return notes;
        }

        private static string? GetCharmHistory(IReadOnlyDictionary<string, string>? infobox)
        {
            string? history = NormalizeCharmText(GetInfoboxValue(infobox, "history"));
            if(!string.IsNullOrWhiteSpace(history))
            {
                string? misplacedStatus = ExtractPrefixedValue(history, "status");
                if(misplacedStatus is null)
                {
                    return history;
                }
            }

            string? notes = NormalizeCharmText(GetInfoboxValue(infobox, "notes"));
            return ExtractPrefixedValue(notes, "history");
        }

        private static string? GetCharmStatus(IReadOnlyDictionary<string, string>? infobox)
        {
            string? status = NormalizeCharmText(GetInfoboxValue(infobox, "status"));
            if(!string.IsNullOrWhiteSpace(status))
            {
                return status;
            }

            string? history = NormalizeCharmText(GetInfoboxValue(infobox, "history"));
            string? misplacedStatus = ExtractPrefixedValue(history, "status");
            if(!string.IsNullOrWhiteSpace(misplacedStatus))
            {
                return misplacedStatus;
            }

            string? notes = NormalizeCharmText(GetInfoboxValue(infobox, "notes"));
            return ExtractPrefixedValue(notes, "status");
        }

        private static string? GetInfoboxValue(IReadOnlyDictionary<string, string>? infobox, string key)
        {
            if(infobox is null ||
               !infobox.TryGetValue(key, out string? value) ||
               string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeCharmText(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Replace("\r", " ")
                                     .Replace("\n", " ")
                                     .Replace("\\n", " ")
                                     .Trim();

            while (normalized.Contains("  ", StringComparison.Ordinal))
            {
                normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
            }

            return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
        }

        private static string? ExtractPrefixedValue(string? value, string key)
        {
            string? normalized = NormalizeCharmText(value);
            if(string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            string prefix = $"| {key} =";
            if(!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string extracted = normalized[prefix.Length..].Trim();
            return string.IsNullOrWhiteSpace(extracted)
            ? null
            : extracted;
        }
    }
}