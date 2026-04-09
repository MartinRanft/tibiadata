using System.Text.Json;
using System.Text.RegularExpressions;

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
    public sealed partial class KeysDataBaseService(
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
                   (!string.IsNullOrWhiteSpace(GetKeyNumber(item, additionalAttributes)) ||
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
                    GetKeyNumber(item, additionalAttributes),
                    null,
                    null,
                    null,
                    null,
                    null,
                    item.Implemented,
                    null,
                    null,
                    null,
                    null,
                    BuildKeyFields(item, additionalAttributes));
            }

            return new KeyInfoboxResponse(
                item.Name,
                item.ActualName,
                GetKeyNumber(item, additionalAttributes),
                GetAdditionalValue(additionalAttributes, "location"),
                GetAdditionalValue(additionalAttributes, "quest"),
                GetAdditionalValue(additionalAttributes, "shortNotes")
                ?? GetAdditionalValue(additionalAttributes, "longNotes")
                ?? GetAdditionalValue(additionalAttributes, "usage"),
                GetAdditionalValue(additionalAttributes, "history"),
                GetAdditionalValue(additionalAttributes, "status"),
                item.Implemented,
                GetAdditionalValue(additionalAttributes, "aka"),
                GetAdditionalValue(additionalAttributes, "origin"),
                GetAdditionalValue(additionalAttributes, "shortNotes"),
                GetAdditionalValue(additionalAttributes, "longNotes"),
                BuildKeyFields(item, additionalAttributes));
        }

        private static string GetKeyName(Item item)
        {
            return item.Name;
        }

        private static IReadOnlyDictionary<string, string>? BuildKeyFields(
            Item item,
            IReadOnlyDictionary<string, string>? additionalAttributes)
        {
            Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase);
            string? keyNumber = GetKeyNumber(item, additionalAttributes);

            AddField(fields, "name", item.Name);
            AddField(fields, "actualname", item.ActualName);
            AddField(fields, "plural", item.Plural);
            AddField(fields, "article", item.Article);
            AddField(fields, "implemented", item.Implemented);
            AddField(fields, "templateType", item.TemplateType);
            AddField(fields, "objectClass", item.ObjectClass);
            AddField(fields, "primarytype", item.PrimaryType);
            AddField(fields, "secondarytype", item.SecondaryType);
            AddField(fields, "npcprice", item.NpcPrice);
            AddField(fields, "npcvalue", item.NpcValue);
            AddField(fields, "value", item.Value);
            AddField(fields, "weight", item.Weight);
            AddField(fields, "wikiUrl", item.WikiUrl);
            AddField(fields, "number", keyNumber);
            AddField(fields, "location", GetAdditionalValue(additionalAttributes, "location"));
            AddField(fields, "quest", GetAdditionalValue(additionalAttributes, "quest"));
            AddField(fields, "aka", GetAdditionalValue(additionalAttributes, "aka"));
            AddField(fields, "origin", GetAdditionalValue(additionalAttributes, "origin"));
            AddField(fields, "shortnotes", GetAdditionalValue(additionalAttributes, "shortNotes"));
            AddField(fields, "longnotes", GetAdditionalValue(additionalAttributes, "longNotes"));
            AddField(fields, "history", GetAdditionalValue(additionalAttributes, "history"));
            AddField(fields, "status", GetAdditionalValue(additionalAttributes, "status"));
            AddField(fields, "buyfrom", GetAdditionalValue(additionalAttributes, "buyFrom"));
            AddField(fields, "sellto", GetAdditionalValue(additionalAttributes, "sellTo"));

            if(additionalAttributes is not null)
            {
                foreach(KeyValuePair<string, string> entry in additionalAttributes)
                {
                    AddField(fields, entry.Key, entry.Value);
                }
            }

            return fields.Count == 0 ? null : fields;
        }

        private static void AddField(IDictionary<string, string> fields, string key, string? value)
        {
            if(!string.IsNullOrWhiteSpace(value))
            {
                fields[key] = value.Trim();
            }
        }

        private static string? BuildKeySummary(IReadOnlyDictionary<string, string>? additionalAttributes)
        {
            string? number = GetAdditionalValue(additionalAttributes, "keyNumber")
                             ?? GetAdditionalValue(additionalAttributes, "number");
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

        private static string? GetKeyNumber(Item item, IReadOnlyDictionary<string, string>? additionalAttributes)
        {
            string? number = GetAdditionalValue(additionalAttributes, "keyNumber")
                             ?? GetAdditionalValue(additionalAttributes, "number");

            if(!string.IsNullOrWhiteSpace(number))
            {
                return number;
            }

            if(string.IsNullOrWhiteSpace(item.Name))
            {
                return null;
            }

            Match match = KeyNumberFromNameRegex().Match(item.Name);
            return match.Success ? match.Groups["number"].Value : null;
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

        [GeneratedRegex(@"Key\s+(?<number>\d+)", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex KeyNumberFromNameRegex();
    }
}
