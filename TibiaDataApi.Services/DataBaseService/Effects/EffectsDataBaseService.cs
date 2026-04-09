using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Effects;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Effects.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Effects
{
    public sealed class EffectsDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IEffectsDataBaseService
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

        public async Task<IReadOnlyList<EffectListItemResponse>?> GetEffectNamesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "effects:names",
                async ct =>
                {
                    List<WikiArticle> articles = await db.WikiArticles
                                                         .AsNoTracking()
                                                         .Where(x => x.ContentType == WikiContentType.Effect)
                                                         .Where(x => !x.IsMissingFromSource)
                                                         .OrderBy(x => x.Title)
                                                         .ToListAsync(ct);

                    return articles.Select(article =>
                                   {
                                       Dictionary<string, string>? infobox = ParseEffectInfobox(article.InfoboxJson);
                                       return IsEffectDetailArticle(article, infobox)
                                       ? MapEffectListItem(article, infobox)
                                       : null;
                                   })
                                   .Where(item => item is not null)
                                   .Cast<EffectListItemResponse>()
                                   .ToList();
                },
                _cacheOptions,
                [CacheTags.Effects],
                cancellationToken);
        }

        public async Task<EffectDetailsResponse?> GetEffectDetailsByNameAsync(string effectName, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"effects:by-name:{effectName}",
                async ct =>
                {
                    string normalizeEffectName = EntityNameNormalizer.Normalize(effectName);

                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Effect)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.NormalizedTitle == normalizeEffectName)
                                                   .SingleOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseEffectInfobox(article.InfoboxJson);

                    return MapEffectDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Effects],
                cancellationToken);
        }

        public async Task<EffectDetailsResponse?> GetEffectDetailsByIdAsync(int effectId, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"effects:by-id:{effectId}",
                async ct =>
                {
                    WikiArticle? article = await db.WikiArticles
                                                   .AsNoTracking()
                                                   .Where(x => x.ContentType == WikiContentType.Effect)
                                                   .Where(x => !x.IsMissingFromSource)
                                                   .Where(x => x.Id == effectId)
                                                   .SingleOrDefaultAsync(ct);
                    if(article is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseEffectInfobox(article.InfoboxJson);

                    return MapEffectDetails(article, infobox);
                },
                _cacheOptions,
                [CacheTags.Effects],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetEffectSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "effects:sync-states",
                async ct =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.Effect)
                                   .Where(x => !x.IsMissingFromSource)
                                   .OrderBy(x => x.Id)
                                   .ThenByDescending(x => x.LastUpdated)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Effects],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetEffectSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"effects:sync-states:bydatetime:{time}",
                async ct =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.Effect)
                                   .Where(x => !x.IsMissingFromSource)
                                   .Where(x => x.LastUpdated >= time)
                                   .OrderBy(x => x.Id)
                                   .ThenByDescending(x => x.LastUpdated)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Effects],
                cancellationToken);
        }

        private static Dictionary<string, string>? ParseEffectInfobox(string? infoboxJson)
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

        private static bool IsEffectDetailArticle(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            string? normalizedTemplate = NormalizeTemplate(article.InfoboxTemplate);
            if(!string.Equals(normalizedTemplate, "Infobox Effect", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(GetInfoboxValue(infobox, "name"));
        }

        private static EffectListItemResponse MapEffectListItem(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new EffectListItemResponse(
                article.Id,
                GetEffectName(article, infobox),
                BuildEffectSummary(infobox, article.Summary),
                article.WikiUrl,
                article.LastUpdated);
        }

        private static EffectDetailsResponse MapEffectDetails(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return new EffectDetailsResponse(
                article.Id,
                GetEffectName(article, infobox),
                BuildEffectSummary(infobox, article.Summary),
                article.PlainTextContent,
                article.RawWikiText,
                CreateStructuredData(article, infobox),
                article.WikiUrl,
                article.LastSeenAt,
                article.LastUpdated);
        }

        private static EffectStructuredDataResponse? CreateStructuredData(
            WikiArticle article,
            IReadOnlyDictionary<string, string>? infobox)
        {
            if(string.IsNullOrWhiteSpace(article.InfoboxTemplate) &&
               (infobox is null || infobox.Count == 0))
            {
                return null;
            }

            return new EffectStructuredDataResponse(
                NormalizeTemplate(article.InfoboxTemplate),
                MapEffectInfobox(infobox));
        }

        private static EffectInfoboxResponse? MapEffectInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new EffectInfoboxResponse(
                GetInfoboxValue(infobox, "name"),
                GetInfoboxValue(infobox, "implemented"),
                GetInfoboxValue(infobox, "primarytype"),
                GetInfoboxValue(infobox, "secondarytype"),
                GetInfoboxValue(infobox, "causes"),
                GetInfoboxValue(infobox, "effectid"),
                GetInfoboxValue(infobox, "effect"),
                GetInfoboxValue(infobox, "lightcolor"),
                GetInfoboxValue(infobox, "lightradius"),
                GetEffectNotes(infobox),
                GetEffectHistory(infobox),
                GetEffectStatus(infobox),
                infobox);
        }

        private static string GetEffectName(WikiArticle article, IReadOnlyDictionary<string, string>? infobox)
        {
            return GetInfoboxValue(infobox, "name")
                   ?? article.Title;
        }

        private static string? BuildEffectSummary(
            IReadOnlyDictionary<string, string>? infobox,
            string? storedSummary)
        {
            string? summary = NormalizeEffectText(storedSummary);
            if(!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }

            string? effect = GetInfoboxValue(infobox, "effect");
            string? causes = GetInfoboxValue(infobox, "causes");
            string? primaryType = GetInfoboxValue(infobox, "primarytype");
            string? secondaryType = GetInfoboxValue(infobox, "secondarytype");
            string? notes = GetEffectNotes(infobox);

            List<string> parts = [];

            if(!string.IsNullOrWhiteSpace(effect))
            {
                parts.Add(effect);
            }
            else if(!string.IsNullOrWhiteSpace(causes))
            {
                parts.Add(causes);
            }

            if(!string.IsNullOrWhiteSpace(primaryType) && !string.IsNullOrWhiteSpace(secondaryType))
            {
                parts.Add($"{primaryType} / {secondaryType}");
            }
            else if(!string.IsNullOrWhiteSpace(primaryType))
            {
                parts.Add(primaryType);
            }
            else if(!string.IsNullOrWhiteSpace(secondaryType))
            {
                parts.Add(secondaryType);
            }

            if(!string.IsNullOrWhiteSpace(notes))
            {
                parts.Add(notes);
            }

            if(parts.Count == 0)
            {
                return null;
            }

            string combined = string.Join(". ", parts.Where(static x => !string.IsNullOrWhiteSpace(x))).Trim();
            if(!combined.EndsWith(".", StringComparison.Ordinal))
            {
                combined += ".";
            }

            return combined.Length <= 280
            ? combined
            : combined[..277].TrimEnd() + "...";
        }

        private static string? GetEffectNotes(IReadOnlyDictionary<string, string>? infobox)
        {
            string? notes = NormalizeEffectText(GetInfoboxValue(infobox, "notes"));
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

        private static string? GetEffectHistory(IReadOnlyDictionary<string, string>? infobox)
        {
            string? history = NormalizeEffectText(GetInfoboxValue(infobox, "history"));
            if(!string.IsNullOrWhiteSpace(history))
            {
                string? misplacedStatus = ExtractPrefixedValue(history, "status");
                if(misplacedStatus is null)
                {
                    return history;
                }
            }

            string? notes = NormalizeEffectText(GetInfoboxValue(infobox, "notes"));
            return ExtractPrefixedValue(notes, "history");
        }

        private static string? GetEffectStatus(IReadOnlyDictionary<string, string>? infobox)
        {
            string? status = NormalizeEffectText(GetInfoboxValue(infobox, "status"));
            if(!string.IsNullOrWhiteSpace(status))
            {
                return status;
            }

            string? history = NormalizeEffectText(GetInfoboxValue(infobox, "history"));
            string? misplacedStatus = ExtractPrefixedValue(history, "status");
            if(!string.IsNullOrWhiteSpace(misplacedStatus))
            {
                return misplacedStatus;
            }

            string? notes = NormalizeEffectText(GetInfoboxValue(infobox, "notes"));
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

        private static string? NormalizeEffectText(string? value)
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

            return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
        }

        private static string? ExtractPrefixedValue(string? value, string key)
        {
            string? normalized = NormalizeEffectText(value);
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
