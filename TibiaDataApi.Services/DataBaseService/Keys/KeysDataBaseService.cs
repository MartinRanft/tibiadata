using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Keys;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.DataBaseService.Keys.Interfaces;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Keys
{
    public sealed class KeysDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IKeysDataBaseService
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

        public async Task<IReadOnlyList<KeyListItemResponse>> GetKeysAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "keys:list",
                async ct =>
                {
                    List<Item> items = await db.Items
                                               .AsNoTracking()
                                               .Where(x => !x.IsMissingFromSource)
                                               .Where(x => x.Category != null && x.Category.Slug == "keys")
                                               .OrderBy(x => x.Name)
                                               .ToListAsync(ct);

                    return items.Select(item => new
                                {
                                    Item = item,
                                    AdditionalAttributes = ParseKeyAdditionalAttributes(item.AdditionalAttributesJson)
                                })
                                .Where(x => IsKeyDetailItem(x.Item, x.AdditionalAttributes))
                                .Select(x => MapKeyListItem(x.Item, x.AdditionalAttributes))
                                .ToList();
                },
                _cacheOptions,
                [CacheTags.Keys, CacheTags.Items, CacheTags.Category("keys")],
                cancellationToken);
        }

        public async Task<KeyDetailsResponse?> GetKeyDetailsByNameAsync(string keyName, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"keys:by-name:{keyName}",
                async ct =>
                {
                    string normalizeName = EntityNameNormalizer.Normalize(keyName);

                    Item? items = await db.Items
                                          .AsNoTracking()
                                          .Where(x => !x.IsMissingFromSource)
                                          .Where(x => x.Category != null && x.Category.Slug == "keys")
                                          .Where(x => x.NormalizedName == normalizeName)
                                          .OrderBy(x => x.Name)
                                          .FirstOrDefaultAsync(ct);

                    if(items is null)
                    {
                        return null;
                    }

                    return MapKeyDetails(items, ParseKeyAdditionalAttributes(items.AdditionalAttributesJson));
                },
                _cacheOptions,
                [CacheTags.Keys, CacheTags.Items, CacheTags.Category("keys")],
                cancellationToken);
        }

        public async Task<KeyDetailsResponse?> GetKeyDetailsByIdAsync(int keyId, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"keys:by-id:{keyId}",
                async ct =>
                {
                    Item? item = await db.Items
                                         .AsNoTracking()
                                         .Where(x => !x.IsMissingFromSource)
                                         .Where(x => x.Category != null && x.Category.Slug == "keys")
                                         .Where(x => x.Id == keyId)
                                         .OrderBy(x => x.Name)
                                         .FirstOrDefaultAsync(ct);

                    if(item is null)
                    {
                        return null;
                    }

                    return MapKeyDetails(item, ParseKeyAdditionalAttributes(item?.AdditionalAttributesJson));
                },
                _cacheOptions,
                [CacheTags.Keys, CacheTags.Items, CacheTags.Category("keys")],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetKeySyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "keys:sync-states",
                async ct =>
                {
                    return await db.Items
                                   .AsNoTracking()
                                   .Where(x => !x.IsMissingFromSource)
                                   .Where(x => x.Category != null && x.Category.Slug == "keys")
                                   .OrderBy(x => x.Id)
                                   .ThenByDescending(x => x.LastUpdated)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       null))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Keys, CacheTags.Items, CacheTags.Category("keys")],
                cancellationToken);
        }

        public async Task<IReadOnlyList<SyncStateResponse>?> GetKeySyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"keys:sync-states-bydatetime:{time:O}",
                async ct =>
                {
                    return await db.Items
                                   .AsNoTracking()
                                   .Where(x => !x.IsMissingFromSource)
                                   .Where(x => x.Category != null && x.Category.Slug == "keys")
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
                [CacheTags.Keys, CacheTags.Items, CacheTags.Category("keys")],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseKeyAdditionalAttributes(string? additionalAttributesJson)
        {
            if(string.IsNullOrWhiteSpace(additionalAttributesJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(additionalAttributesJson, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool IsKeyDetailItem(Item item, IReadOnlyDictionary<string, string>? additionalAttributes)
        {
            return !string.IsNullOrWhiteSpace(item.Name) &&
                   (!string.IsNullOrWhiteSpace(GetAdditionalValue(additionalAttributes, "keyNumber")) ||
                    !string.IsNullOrWhiteSpace(GetAdditionalValue(additionalAttributes, "usage")) ||
                    !string.IsNullOrWhiteSpace(GetAdditionalValue(additionalAttributes, "doorLevel")) ||
                    !string.IsNullOrWhiteSpace(item.ActualName));
        }

        private static KeyListItemResponse MapKeyListItem(Item item, IReadOnlyDictionary<string, string>? additionalAttributes)
        {
            return new KeyListItemResponse(
                item.Id,
                GetKeyName(item),
                BuildKeySummary(additionalAttributes),
                item.WikiUrl,
                item.LastUpdated);
        }

        private static KeyDetailsResponse MapKeyDetails(Item item, IReadOnlyDictionary<string, string>? additionalAttributes)
        {
            return new KeyDetailsResponse(
                item.Id,
                GetKeyName(item),
                BuildKeySummary(additionalAttributes),
                null,
                null,
                CreateStructuredData(item, additionalAttributes),
                item.WikiUrl,
                item.LastSeenAt,
                item.LastUpdated);
        }

        private static KeyStructuredDataResponse? CreateStructuredData(
            Item item,
            IReadOnlyDictionary<string, string>? additionalAttributes)
        {
            if(string.IsNullOrWhiteSpace(item.TemplateType) &&
               (additionalAttributes is null || additionalAttributes.Count == 0))
            {
                return null;
            }

            return new KeyStructuredDataResponse(
                NormalizeTemplate(item.TemplateType) ?? "Item",
                MapKeyInfobox(item, additionalAttributes));
        }

        private static KeyInfoboxResponse? MapKeyInfobox(Item item, IReadOnlyDictionary<string, string>? additionalAttributes)
        {
            if(additionalAttributes is null || additionalAttributes.Count == 0)
            {
                return new KeyInfoboxResponse(
                    item.Name,
                    item.ActualName,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    item.Implemented);
            }

            return new KeyInfoboxResponse(
                item.Name,
                item.ActualName,
                GetAdditionalValue(additionalAttributes, "keyNumber"),
                null,
                null,
                GetAdditionalValue(additionalAttributes, "usage"),
                null,
                null,
                item.Implemented);
        }

        private static string GetKeyName(Item item)
        {
            return item.Name;
        }

        private static string? BuildKeySummary(IReadOnlyDictionary<string, string>? additionalAttributes)
        {
            string? number = GetAdditionalValue(additionalAttributes, "keyNumber");
            string? doorLevel = GetAdditionalValue(additionalAttributes, "doorLevel");
            string? usage = GetAdditionalValue(additionalAttributes, "usage");

            List<string> parts = [];

            if(!string.IsNullOrWhiteSpace(number))
            {
                parts.Add($"Key {number}.");
            }

            if(!string.IsNullOrWhiteSpace(doorLevel))
            {
                parts.Add($"Door level: {doorLevel}.");
            }

            if(!string.IsNullOrWhiteSpace(usage))
            {
                parts.Add(usage);
            }

            if(parts.Count == 0)
            {
                return null;
            }

            string combined = string.Join(" ", parts).Trim();
            return combined.Length <= 280
            ? combined
            : combined[..277].TrimEnd() + "...";
        }

        private static string? GetAdditionalValue(IReadOnlyDictionary<string, string>? additionalAttributes, string key)
        {
            if(additionalAttributes is null ||
               !additionalAttributes.TryGetValue(key, out string? value))
            {
                return null;
            }

            return NormalizeKeyText(value);
        }

        private static string? NormalizeTemplate(string? template)
        {
            if(string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template.Replace('_', ' ').Trim();
        }

        private static string? NormalizeKeyText(string? value)
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

            if(string.IsNullOrWhiteSpace(normalized) ||
               string.Equals(normalized, "?", StringComparison.Ordinal) ||
               string.Equals(normalized, "}}", StringComparison.Ordinal))
            {
                return null;
            }

            return normalized;
        }
    }
}