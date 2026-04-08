using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Spells;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Spells.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Spells
{
    public sealed class SpellsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : ISpellsDataBaseService
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

        public async Task<IReadOnlyList<SpellListItemResponse>> GetSpellsAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "spells:list",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Spell)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseSpellInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsSpellDetailArticle(x.Article, x.Infobox))
                                   .Select(x => MapSpellListItem(x.Article, x.Infobox))
                                   .OrderBy(x => x.Name)
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Spells],
                cancellationToken);
        }

        public async Task<SpellDetailsResponse?> GetSpellDetailsByNameAsync(string spellName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(spellName);
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            string cacheKey = ($"spells:by-name:{normalizedName}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    int spellId = await db.WikiArticles
                                          .AsNoTracking()
                                          .Where(x => x.ContentType == WikiContentType.Spell)
                                          .Where(x => !x.IsMissingFromSource)
                                          .Where(x => x.NormalizedTitle == normalizedName)
                                          .Select(x => x.Id)
                                          .FirstOrDefaultAsync(ct);

                    if(spellId <= 0)
                    {
                        return null;
                    }

                    return await GetSpellDetailsByIdAsync(spellId, ct);
                },
                _cacheOptions,
                [CacheTags.Spells],
                cancellationToken);
        }

        public async Task<SpellDetailsResponse?> GetSpellDetailsByIdAsync(int spellId, CancellationToken cancellationToken = default)
        {
            if(spellId <= 0)
            {
                return null;
            }

            string cacheKey = ($"spells:by-id:{spellId}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Spell)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == spellId)
                                                   .FirstOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseSpellInfobox(article.InfoboxJson);
                    if(!IsSpellDetailArticle(article, infobox))
                    {
                        return null;
                    }

                    return MapSpellDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Spells],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetSpellSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "spells:sync-states",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Spell)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseSpellInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsSpellDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Spells],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetSpellSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            string cacheKey = ($"spells:sync-states:{time:O}").ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Spell)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .Where(x => x.LastUpdated >= time)
                                                         .OrderByDescending(x => x.LastUpdated)
                                                         .ThenByDescending(x => x.Id)
                                                         .ToListAsync(ct);

                    return articles.Select(article => new
                                   {
                                       Article = article,
                                       Infobox = ParseSpellInfobox(article.InfoboxJson)
                                   })
                                   .Where(x => IsSpellDetailArticle(x.Article, x.Infobox))
                                   .Select(x => new SyncStateResponse(x.Article.Id, x.Article.LastUpdated, x.Article.LastSeenAt))
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Spells],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseSpellInfobox(string? infoboxJson)
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

        private static bool IsSpellDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            return string.Equals(normalizedTemplate, "Infobox Spell", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static SpellListItemResponse MapSpellListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new SpellListItemResponse(
                article.Id,
                GetSpellName(article, infobox),
                BuildSpellSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static SpellDetailsResponse MapSpellDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new SpellDetailsResponse(
                article.Id,
                GetSpellName(article, infobox),
                BuildSpellSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static SpellStructuredDataResponse? CreateStructuredData(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) && (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new SpellStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapSpellInfobox(infobox));
        }

        private static SpellInfoboxResponse? MapSpellInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new SpellInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "premium"),
                GetInfoboxValue(infobox, "subclass"),
                GetInfoboxValue(infobox, "voc"),
                GetInfoboxValue(infobox, "mana"),
                GetInfoboxValue(infobox, "soul"),
                GetInfoboxValue(infobox, "type"),
                GetInfoboxValue(infobox, "spellid"),
                GetInfoboxValue(infobox, "levelrequired"),
                GetInfoboxValue(infobox, "cooldown"),
                GetInfoboxValue(infobox, "cooldowngroup"),
                GetInfoboxValue(infobox, "cooldowngroup2"),
                GetInfoboxValue(infobox, "words"),
                GetInfoboxValue(infobox, "effect"),
                GetInfoboxValue(infobox, "damagetype"),
                GetInfoboxValue(infobox, "animation"),
                GetInfoboxValue(infobox, "basepower"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "notes"),
                GetInfoboxValue(infobox, "history"));
        }

        private static string GetSpellName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name") ?? article.Title;
        }

        private static string? BuildSpellSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? summary = NormalizeSpellText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            List<string> parts = [];
            string? voc = GetInfoboxValue(infobox, "voc");
            string? words = GetInfoboxValue(infobox, "words");
            string? mana = GetInfoboxValue(infobox, "mana");
            string? level = GetInfoboxValue(infobox, "levelrequired");

            if(!string.IsNullOrWhiteSpace(voc))
            {
                parts.Add($"Voc: {voc}.");
            }

            if(!string.IsNullOrWhiteSpace(words))
            {
                parts.Add($"Words: {words}.");
            }

            if(!string.IsNullOrWhiteSpace(mana))
            {
                parts.Add($"Mana: {mana}.");
            }
            else if(!string.IsNullOrWhiteSpace(level))
            {
                parts.Add($"Level: {level}.");
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

            return NormalizeSpellText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeSpellText(string? value)
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