using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Npcs;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Npcs.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Npcs
{
    public sealed class NpcsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : INpcsDataBaseService
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

        public async Task<IReadOnlyList<NpcListItemResponse>> GetNpcsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "npcs:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Npc)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseNpcInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsNpcDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapNpcListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Npcs],
                cancellationToken);
        }

        public async Task<NpcDetailsResponse?> GetNpcDetailsByNameAsync(string npcName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(npcName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"npcs:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int npcId = await db.WikiArticles
                                        .AsNoTracking()
                                        .Where(x => x.ContentType == WikiContentType.Npc)
                                        .Where(x => !x.IsMissingFromSource)
                                        .Where(x => x.NormalizedTitle == normalizedName)
                                        .Select(x => x.Id)
                                        .FirstOrDefaultAsync(ct);

                    if(npcId <= 0)
                    {
                        return null;
                    }

                    return await GetNpcDetailsByIdAsync(npcId, ct);
                },
                _cacheOptions,
                [CacheTags.Npcs],
                cancellationToken);
        }

        public async Task<NpcDetailsResponse?> GetNpcDetailsByIdAsync(int npcId, CancellationToken cancellationToken = default)
        {
            if(npcId <= 0)
            {
                return null;
            }

            string cacheKey = ($"npcs:by-id:{npcId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Npc)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == npcId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseNpcInfobox(article.InfoboxJson);
                    if(!IsNpcDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapNpcDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Npcs],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetNpcSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "npcs:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Npc)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseNpcInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsNpcDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Npcs],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetNpcSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"npcs:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Npc)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseNpcInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsNpcDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Npcs],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseNpcInfobox(string? infoboxJson)
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

        private static bool IsNpcDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            return string.Equals(normalizedTemplate, "Infobox NPC", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static NpcListItemResponse MapNpcListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new NpcListItemResponse(
                article.Id,
                GetNpcName(article, infobox),
                BuildNpcSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static NpcDetailsResponse MapNpcDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new NpcDetailsResponse(
                article.Id,
                GetNpcName(article, infobox),
                BuildNpcSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static NpcStructuredDataResponse? CreateStructuredData(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) && (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new NpcStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapNpcInfobox(infobox));
        }

        private static NpcInfoboxResponse? MapNpcInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new NpcInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "actualname"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "city"),
                GetInfoboxValue(infobox, "race"),
                GetInfoboxValue(infobox, "job"),
                GetInfoboxValue(infobox, "job2"),
                GetInfoboxValue(infobox, "gender"),
                GetInfoboxValue(infobox, "buysell"),
                GetInfoboxValue(infobox, "location"),
                GetInfoboxValue(infobox, "subarea"),
                GetInfoboxValue(infobox, "sounds"),
                GetInfoboxValue(infobox, "notes"),
                GetInfoboxValue(infobox, "status"),
                GetInfoboxValue(infobox, "posx"),
                GetInfoboxValue(infobox, "posy"),
                GetInfoboxValue(infobox, "posz"),
                GetInfoboxValue(infobox, "posx2"),
                GetInfoboxValue(infobox, "posy2"),
                GetInfoboxValue(infobox, "posz2"),
                infobox);
        }

        private static string GetNpcName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name") ?? article.Title;
        }

        private static string? BuildNpcSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? summary = NormalizeNpcText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            List<string> parts = [];
            string? city = GetInfoboxValue(infobox, "city");
            string? job = GetInfoboxValue(infobox, "job");
            string? location = GetInfoboxValue(infobox, "location");
            string? notes = GetInfoboxValue(infobox, "notes");

            if(!string.IsNullOrWhiteSpace(city))
            {
                parts.Add($"City: {city}.");
            }

            if(!string.IsNullOrWhiteSpace(job))
            {
                parts.Add($"Job: {job}.");
            }

            if(!string.IsNullOrWhiteSpace(location))
            {
                parts.Add($"Location: {location}.");
            }
            else if(!string.IsNullOrWhiteSpace(notes))
            {
                parts.Add(notes);
            }

            if(parts.Count == 0)
            {
                return null;
            }

            string combined = string.Join(" ", parts).Trim();
            return combined.Length <= 280 ? combined : combined[..277].TrimEnd() + "...";
        }

        private static string? GetInfoboxValue(IReadOnlyDictionary<string, string>? infobox, string key)
        {
            if(infobox is null || !infobox.TryGetValue(key, out string? value))
            {
                return null;
            }

            return NormalizeNpcText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeNpcText(string? value)
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
