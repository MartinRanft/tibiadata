using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Meta;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Meta.Interfaces;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.DataBaseService.Meta
{
    public sealed class MetaDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IMetaDataBaseService
    {
        private const int PublicSchemaVersion = 1;
        private static readonly HashSet<WikiContentType> SupportedDeltaWikiContentTypes =
        [
            WikiContentType.Achievement,
            WikiContentType.BookText,
            WikiContentType.Building,
            WikiContentType.Charm,
            WikiContentType.Corpse,
            WikiContentType.Effect,
            WikiContentType.HuntingPlace,
            WikiContentType.Location,
            WikiContentType.Missile,
            WikiContentType.Mount,
            WikiContentType.Npc,
            WikiContentType.Object,
            WikiContentType.Outfit,
            WikiContentType.Quest,
            WikiContentType.Spell,
            WikiContentType.Street
        ];

        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<ApiVersionResponse> GetApiVersionAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "meta:version",
                async ct =>
                {
                    AggregateState state = await GetAggregateStateAsync(ct);

                    DateTime generatedAtUtc = DateTime.UtcNow;
                    DateTime? latestDataUpdateUtc = GetLatestUpdateUtc(state.ItemState, state.WikiArticleState, state.CreatureState, state.CategoryState, state.AssetState);
                    string dataVersion = CreateDataVersion(state.ItemState, state.WikiArticleState, state.CreatureState, state.CategoryState, state.AssetState);

                    return new ApiVersionResponse(
                        PublicSchemaVersion,
                        dataVersion,
                        generatedAtUtc,
                        latestDataUpdateUtc,
                        state.ItemState.Count,
                        state.WikiArticleState.Count,
                        state.CreatureState.Count,
                        state.CategoryState.Count,
                        state.AssetState.Count);
                },
                _cacheOptions,
                [CacheTags.Items, CacheTags.WikiArticles, CacheTags.Creatures, CacheTags.Categories, CacheTags.Assets],
                cancellationToken);
        }

        public async Task<ApiSnapshotResponse> GetApiSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "meta:snapshot",
                async ct =>
                {
                    AggregateState state = await GetAggregateStateAsync(ct);
                    DateTime generatedAtUtc = DateTime.UtcNow;
                    DateTime? latestDataUpdateUtc = GetLatestUpdateUtc(state.ItemState, state.WikiArticleState, state.CreatureState, state.CategoryState, state.AssetState);
                    string dataVersion = CreateDataVersion(state.ItemState, state.WikiArticleState, state.CreatureState, state.CategoryState, state.AssetState);

                    List<ApiSnapshotResourceResponse> resources =
                    [
                        CreateWikiArticleResource("achievements", "Achievements", state.ArticleStates, WikiContentType.Achievement),
                        CreateWikiArticleResource("books", "Books", state.ArticleStates, WikiContentType.BookText),
                        CreateWikiArticleResource("buildings", "Buildings", state.ArticleStates, WikiContentType.Building, "/api/v1/buildings/city/{city}"),
                        CreateWikiArticleResource("charms", "Charms", state.ArticleStates, WikiContentType.Charm),
                        CreateWikiArticleResource("corpses", "Corpses", state.ArticleStates, WikiContentType.Corpse),
                        CreateCreatureResource(state.CreatureState),
                        CreateWikiArticleResource("effects", "Effects", state.ArticleStates, WikiContentType.Effect),
                        CreateHuntingPlaceResource(state.ArticleStates),
                        CreateItemResource(state.ItemState),
                        CreateKeyResource(state.KeyState),
                        CreateWikiArticleResource("locations", "Locations", state.ArticleStates, WikiContentType.Location),
                        CreateWikiArticleResource("missiles", "Missiles", state.ArticleStates, WikiContentType.Missile),
                        CreateWikiArticleResource("mounts", "Mounts", state.ArticleStates, WikiContentType.Mount),
                        CreateWikiArticleResource("npcs", "NPCs", state.ArticleStates, WikiContentType.Npc),
                        CreateWikiArticleResource("objects", "Objects", state.ArticleStates, WikiContentType.Object),
                        CreateWikiArticleResource("outfits", "Outfits", state.ArticleStates, WikiContentType.Outfit),
                        CreateWikiArticleResource("quests", "Quests", state.ArticleStates, WikiContentType.Quest),
                        CreateWikiArticleResource("spells", "Spells", state.ArticleStates, WikiContentType.Spell),
                        CreateWikiArticleResource("streets", "Streets", state.ArticleStates, WikiContentType.Street),
                        CreateCategoryResource(state.CategoryState),
                        CreateAssetResource(state.AssetState)
                    ];

                    return new ApiSnapshotResponse(
                        PublicSchemaVersion,
                        dataVersion,
                        generatedAtUtc,
                        latestDataUpdateUtc,
                        resources);
                },
                _cacheOptions,
                [CacheTags.Items, CacheTags.Keys, CacheTags.WikiArticles, CacheTags.Creatures, CacheTags.Categories, CacheTags.Assets],
                cancellationToken);
        }

        public async Task<ApiDeltaFeedResponse> GetApiDeltaFeedAsync(
            DateTime sinceUtc,
            IReadOnlyCollection<string>? resources = null,
            int limit = 250,
            CancellationToken cancellationToken = default)
        {
            DateTime normalizedSinceUtc = DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc);
            int normalizedLimit = Math.Clamp(limit, 1, 500);
            HashSet<string> resourceFilter = resources is null || resources.Count == 0
            ? []
            : resources.Where(x => !string.IsNullOrWhiteSpace(x))
                       .Select(x => x.Trim().ToLowerInvariant())
                       .ToHashSet(StringComparer.OrdinalIgnoreCase);
            string filterKey = resourceFilter.Count == 0
            ? "all"
            : string.Join(",", resourceFilter.OrderBy(x => x, StringComparer.Ordinal));

            return await hybridCache.GetOrCreateAsync(
                $"meta:delta:{normalizedSinceUtc:O}:{filterKey}:{normalizedLimit}".ToLowerInvariant(),
                async ct =>
                {
                    AggregateState state = await GetAggregateStateAsync(ct);
                    List<ApiDeltaEntryResponse> changes = [];

                    if(ShouldIncludeResource(resourceFilter, "items"))
                    {
                        changes.AddRange(await GetItemDeltaEntriesAsync(normalizedSinceUtc, ct));
                    }

                    if(ShouldIncludeResource(resourceFilter, "keys"))
                    {
                        changes.AddRange(await GetKeyDeltaEntriesAsync(normalizedSinceUtc, ct));
                    }

                    if(ShouldIncludeResource(resourceFilter, "creatures"))
                    {
                        changes.AddRange(await GetCreatureDeltaEntriesAsync(normalizedSinceUtc, ct));
                    }

                    if(ShouldIncludeResource(resourceFilter, "categories"))
                    {
                        changes.AddRange(await GetCategoryDeltaEntriesAsync(normalizedSinceUtc, ct));
                    }

                    if(ShouldIncludeResource(resourceFilter, "assets"))
                    {
                        changes.AddRange(await GetAssetDeltaEntriesAsync(normalizedSinceUtc, ct));
                    }

                    changes.AddRange(await GetWikiArticleDeltaEntriesAsync(normalizedSinceUtc, resourceFilter, ct));

                    List<ApiDeltaEntryResponse> orderedChanges = changes
                                                                  .OrderByDescending(x => x.UpdatedAtUtc)
                                                                  .ThenBy(x => x.Resource, StringComparer.Ordinal)
                                                                  .ThenBy(x => x.Identifier, StringComparer.OrdinalIgnoreCase)
                                                                  .ToList();

                    DateTime generatedAtUtc = DateTime.UtcNow;
                    string dataVersion = CreateDataVersion(state.ItemState, state.WikiArticleState, state.CreatureState, state.CategoryState, state.AssetState);
                    DateTime? latestChangeUtc = orderedChanges.Count == 0
                    ? null
                    : orderedChanges[0].UpdatedAtUtc;
                    bool hasMore = orderedChanges.Count > normalizedLimit;
                    List<ApiDeltaEntryResponse> page = orderedChanges.Take(normalizedLimit).ToList();

                    return new ApiDeltaFeedResponse(
                        PublicSchemaVersion,
                        dataVersion,
                        generatedAtUtc,
                        normalizedSinceUtc,
                        latestChangeUtc,
                        page.Count,
                        hasMore,
                        page);
                },
                _cacheOptions,
                [CacheTags.Items, CacheTags.Keys, CacheTags.WikiArticles, CacheTags.Creatures, CacheTags.Categories, CacheTags.Assets],
                cancellationToken);
        }

        private async Task<AggregateState> GetAggregateStateAsync(CancellationToken cancellationToken)
        {
            SourceState itemState = await GetItemStateAsync(cancellationToken);
            SourceState wikiArticleState = await GetWikiArticleStateAsync(cancellationToken);
            SourceState creatureState = await GetCreatureStateAsync(cancellationToken);
            SourceState categoryState = await GetCategoryStateAsync(cancellationToken);
            SourceState assetState = await GetAssetStateAsync(cancellationToken);
            SourceState keyState = await GetKeyStateAsync(cancellationToken);
            Dictionary<WikiContentType, SourceState> articleStates = await GetArticleStatesAsync(cancellationToken);

            return new AggregateState(
                itemState,
                wikiArticleState,
                creatureState,
                categoryState,
                assetState,
                keyState,
                articleStates);
        }

        private async Task<SourceState> GetItemStateAsync(CancellationToken cancellationToken)
        {
            return await db.Items
                           .AsNoTracking()
                           .Where(x => !x.IsMissingFromSource)
                           .GroupBy(_ => 1)
                           .Select(group => new SourceState(
                               group.Count(),
                               group.Max(x => (DateTime?)x.LastUpdated)))
                           .SingleOrDefaultAsync(cancellationToken)
                   ?? SourceState.Empty;
        }

        private async Task<SourceState> GetWikiArticleStateAsync(CancellationToken cancellationToken)
        {
            return await db.WikiArticles
                           .AsNoTracking()
                           .Where(x => !x.IsMissingFromSource)
                           .GroupBy(_ => 1)
                           .Select(group => new SourceState(
                               group.Count(),
                               group.Max(x => (DateTime?)x.LastUpdated)))
                           .SingleOrDefaultAsync(cancellationToken)
                   ?? SourceState.Empty;
        }

        private async Task<SourceState> GetCreatureStateAsync(CancellationToken cancellationToken)
        {
            return await db.Creatures
                           .AsNoTracking()
                           .GroupBy(_ => 1)
                           .Select(group => new SourceState(
                               group.Count(),
                               group.Max(x => (DateTime?)x.LastUpdated)))
                           .SingleOrDefaultAsync(cancellationToken)
                   ?? SourceState.Empty;
        }

        private async Task<SourceState> GetCategoryStateAsync(CancellationToken cancellationToken)
        {
            return await db.WikiCategories
                           .AsNoTracking()
                           .Where(x => x.IsActive)
                           .GroupBy(_ => 1)
                           .Select(group => new SourceState(
                               group.Count(),
                               group.Max(x => (DateTime?)x.UpdatedAt)))
                           .SingleOrDefaultAsync(cancellationToken)
                   ?? SourceState.Empty;
        }

        private async Task<SourceState> GetAssetStateAsync(CancellationToken cancellationToken)
        {
            return await db.Assets
                           .AsNoTracking()
                           .GroupBy(_ => 1)
                           .Select(group => new SourceState(
                               group.Count(),
                               group.Max(x => (DateTime?)x.UpdatedAt)))
                           .SingleOrDefaultAsync(cancellationToken)
                   ?? SourceState.Empty;
        }

        private async Task<SourceState> GetKeyStateAsync(CancellationToken cancellationToken)
        {
            return await db.Items
                           .AsNoTracking()
                           .Where(x => !x.IsMissingFromSource)
                           .Where(x => x.Category != null && x.Category.Slug == "keys")
                           .GroupBy(_ => 1)
                           .Select(group => new SourceState(
                               group.Count(),
                               group.Max(x => (DateTime?)x.LastUpdated)))
                           .SingleOrDefaultAsync(cancellationToken)
                   ?? SourceState.Empty;
        }

        private async Task<Dictionary<WikiContentType, SourceState>> GetArticleStatesAsync(CancellationToken cancellationToken)
        {
            List<ArticleStateRow> rows = await db.WikiArticles
                                                 .AsNoTracking()
                                                 .Where(x => !x.IsMissingFromSource)
                                                 .GroupBy(x => x.ContentType)
                                                 .Select(group => new ArticleStateRow(
                                                     group.Key,
                                                     group.Count(),
                                                     group.Max(x => (DateTime?)x.LastUpdated)))
                                                 .ToListAsync(cancellationToken);

            return rows.ToDictionary(
                row => row.ContentType,
                row => new SourceState(row.Count, row.LatestUpdatedAtUtc));
        }

        private async Task<List<ApiDeltaEntryResponse>> GetItemDeltaEntriesAsync(DateTime sinceUtc, CancellationToken cancellationToken)
        {
            List<ItemDeltaRow> rows = await db.Items
                                              .AsNoTracking()
                                              .Where(x => x.LastUpdated >= sinceUtc)
                                              .Select(x => new ItemDeltaRow(
                                                  x.Id,
                                                  x.Name,
                                                  x.LastUpdated,
                                                  x.IsMissingFromSource))
                                              .ToListAsync(cancellationToken);

            return rows.Select(row => CreateNamedDeltaEntry(
                "items",
                row.Id,
                row.Name,
                row.LastUpdated,
                row.IsMissingFromSource,
                $"/api/v1/items/{EscapeRouteSegment(row.Name)}",
                $"/api/v1/items/{row.Id}"))
                       .ToList();
        }

        private async Task<List<ApiDeltaEntryResponse>> GetKeyDeltaEntriesAsync(DateTime sinceUtc, CancellationToken cancellationToken)
        {
            List<ItemDeltaRow> rows = await db.Items
                                              .AsNoTracking()
                                              .Where(x => x.LastUpdated >= sinceUtc)
                                              .Where(x => x.Category != null && x.Category.Slug == "keys")
                                              .Select(x => new ItemDeltaRow(
                                                  x.Id,
                                                  x.Name,
                                                  x.LastUpdated,
                                                  x.IsMissingFromSource))
                                              .ToListAsync(cancellationToken);

            return rows.Select(row => CreateNamedDeltaEntry(
                "keys",
                row.Id,
                row.Name,
                row.LastUpdated,
                row.IsMissingFromSource,
                $"/api/v1/keys/{EscapeRouteSegment(row.Name)}",
                $"/api/v1/keys/{row.Id}"))
                       .ToList();
        }

        private async Task<List<ApiDeltaEntryResponse>> GetCreatureDeltaEntriesAsync(DateTime sinceUtc, CancellationToken cancellationToken)
        {
            List<CreatureDeltaRow> rows = await db.Creatures
                                                  .AsNoTracking()
                                                  .Where(x => x.LastUpdated >= sinceUtc)
                                                  .Select(x => new CreatureDeltaRow(
                                                      x.Id,
                                                      x.Name,
                                                      x.LastUpdated))
                                                  .ToListAsync(cancellationToken);

            return rows.Select(row => CreateNamedDeltaEntry(
                "creatures",
                row.Id,
                row.Name,
                row.LastUpdated,
                false,
                $"/api/v1/creatures/{EscapeRouteSegment(row.Name)}",
                $"/api/v1/creatures/{row.Id}"))
                       .ToList();
        }

        private async Task<List<ApiDeltaEntryResponse>> GetCategoryDeltaEntriesAsync(DateTime sinceUtc, CancellationToken cancellationToken)
        {
            List<CategoryDeltaRow> rows = await db.WikiCategories
                                                  .AsNoTracking()
                                                  .Where(x => x.UpdatedAt >= sinceUtc)
                                                  .Select(x => new CategoryDeltaRow(
                                                      x.Id,
                                                      x.Slug,
                                                      x.UpdatedAt,
                                                      !x.IsActive))
                                                  .ToListAsync(cancellationToken);

            return rows.Select(row => CreateNamedDeltaEntry(
                "categories",
                row.Id,
                row.Slug,
                row.UpdatedAt,
                row.IsDeleted,
                $"/api/v1/categories/{EscapeRouteSegment(row.Slug)}",
                $"/api/v1/categories/{row.Id}"))
                       .ToList();
        }

        private async Task<List<ApiDeltaEntryResponse>> GetAssetDeltaEntriesAsync(DateTime sinceUtc, CancellationToken cancellationToken)
        {
            List<AssetDeltaRow> rows = await db.Assets
                                               .AsNoTracking()
                                               .Where(x => x.UpdatedAt >= sinceUtc)
                                               .Select(x => new AssetDeltaRow(
                                                   x.Id,
                                                   x.StorageKey,
                                                   x.UpdatedAt))
                                               .ToListAsync(cancellationToken);

            return rows.Select(row => new ApiDeltaEntryResponse(
                "assets",
                row.Id,
                row.StorageKey,
                row.UpdatedAt,
                "upsert",
                $"/api/v1/assets/metadata/{row.Id}",
                [
                    $"/api/v1/assets/metadata/{row.Id}",
                    $"/api/v1/assets/{row.Id}"
                ]))
                       .ToList();
        }

        private async Task<List<ApiDeltaEntryResponse>> GetWikiArticleDeltaEntriesAsync(
            DateTime sinceUtc,
            IReadOnlyCollection<string> resourceFilter,
            CancellationToken cancellationToken)
        {
            List<WikiArticleDeltaRow> rows = await db.WikiArticles
                                                     .AsNoTracking()
                                                     .Where(x => x.LastUpdated >= sinceUtc)
                                                     .Where(x => SupportedDeltaWikiContentTypes.Contains(x.ContentType))
                                                     .Select(x => new WikiArticleDeltaRow(
                                                         x.Id,
                                                         x.ContentType,
                                                         x.Title,
                                                         x.LastUpdated,
                                                         x.IsMissingFromSource))
                                                     .ToListAsync(cancellationToken);

            return rows.Where(row => ShouldIncludeResource(resourceFilter, GetResourceKey(row.ContentType)))
                       .Select(row =>
                       {
                           string resource = GetResourceKey(row.ContentType);
                           return CreateNamedDeltaEntry(
                               resource,
                               row.Id,
                               row.Title,
                               row.LastUpdated,
                               row.IsDeleted,
                               $"/api/v1/{resource}/{EscapeRouteSegment(row.Title)}",
                               $"/api/v1/{resource}/{row.Id}");
                       })
                       .ToList();
        }

        private static ApiDeltaEntryResponse CreateNamedDeltaEntry(
            string resource,
            int id,
            string identifier,
            DateTime updatedAtUtc,
            bool isDeleted,
            string route,
            string byIdRoute)
        {
            return new ApiDeltaEntryResponse(
                resource,
                id,
                identifier,
                updatedAtUtc,
                isDeleted ? "delete" : "upsert",
                route,
                [route, byIdRoute]);
        }

        private static bool ShouldIncludeResource(IReadOnlyCollection<string> resourceFilter, string resource)
        {
            return resourceFilter.Count == 0 || resourceFilter.Contains(resource);
        }

        private static string GetResourceKey(WikiContentType contentType)
        {
            return contentType switch
            {
                WikiContentType.Achievement => "achievements",
                WikiContentType.BookText => "books",
                WikiContentType.Building => "buildings",
                WikiContentType.Charm => "charms",
                WikiContentType.Corpse => "corpses",
                WikiContentType.Effect => "effects",
                WikiContentType.HuntingPlace => "hunting-places",
                WikiContentType.Location => "locations",
                WikiContentType.Missile => "missiles",
                WikiContentType.Mount => "mounts",
                WikiContentType.Npc => "npcs",
                WikiContentType.Object => "objects",
                WikiContentType.Outfit => "outfits",
                WikiContentType.Quest => "quests",
                WikiContentType.Spell => "spells",
                WikiContentType.Street => "streets",
                _ => string.Empty
            };
        }

        private static string EscapeRouteSegment(string value)
        {
            return Uri.EscapeDataString(value);
        }

        private static DateTime? GetLatestUpdateUtc(params SourceState[] states)
        {
            DateTime? latest = null;

            foreach(SourceState state in states)
            {
                if(!state.LatestUpdatedAtUtc.HasValue)
                {
                    continue;
                }

                if(!latest.HasValue || state.LatestUpdatedAtUtc.Value > latest.Value)
                {
                    latest = state.LatestUpdatedAtUtc.Value;
                }
            }

            return latest;
        }

        private static string CreateDataVersion(params SourceState[] states)
        {
            StringBuilder builder = new();
            builder.Append("schema=").Append(PublicSchemaVersion);

            foreach(SourceState state in states)
            {
                builder.Append('|')
                       .Append(state.Count)
                       .Append(':')
                       .Append(state.LatestUpdatedAtUtc?.ToUniversalTime().ToString("O") ?? "null");
            }

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static ApiSnapshotResourceResponse CreateWikiArticleResource(
            string key,
            string displayName,
            IReadOnlyDictionary<WikiContentType, SourceState> articleStates,
            WikiContentType contentType,
            string? extraRoute = null)
        {
            SourceState state = articleStates.TryGetValue(contentType, out SourceState? resolved)
            ? resolved
            : SourceState.Empty;

            string baseRoute = $"/api/v1/{key}";
            List<string> relatedRoutes =
            [
                $"{baseRoute}/list",
                $"{baseRoute}/{{name}}",
                $"{baseRoute}/{{id}}",
                $"{baseRoute}/sync",
                $"{baseRoute}/sync/by-date"
            ];

            if(!string.IsNullOrWhiteSpace(extraRoute))
            {
                relatedRoutes.Add(extraRoute);
            }

            return new ApiSnapshotResourceResponse(
                key,
                displayName,
                state.Count,
                state.LatestUpdatedAtUtc,
                $"{baseRoute}/list",
                $"{baseRoute}/{{name}}",
                $"{baseRoute}/{{id}}",
                $"{baseRoute}/sync",
                $"{baseRoute}/sync/by-date",
                relatedRoutes);
        }

        private static ApiSnapshotResourceResponse CreateCreatureResource(SourceState state)
        {
            const string baseRoute = "/api/v1/creatures";

            return new ApiSnapshotResourceResponse(
                "creatures",
                "Creatures",
                state.Count,
                state.LatestUpdatedAtUtc,
                baseRoute,
                $"{baseRoute}/{{name}}",
                $"{baseRoute}/{{id}}",
                $"{baseRoute}/sync",
                $"{baseRoute}/sync/by-date",
                [
                    baseRoute,
                    $"{baseRoute}/list",
                    $"{baseRoute}/{{name}}",
                    $"{baseRoute}/{{id}}",
                    $"{baseRoute}/sync",
                    $"{baseRoute}/sync/by-date"
                ]);
        }

        private static ApiSnapshotResourceResponse CreateHuntingPlaceResource(IReadOnlyDictionary<WikiContentType, SourceState> articleStates)
        {
            SourceState state = articleStates.TryGetValue(WikiContentType.HuntingPlace, out SourceState? resolved)
            ? resolved
            : SourceState.Empty;
            const string baseRoute = "/api/v1/hunting-places";

            return new ApiSnapshotResourceResponse(
                "hunting-places",
                "Hunting Places",
                state.Count,
                state.LatestUpdatedAtUtc,
                $"{baseRoute}/list",
                $"{baseRoute}/{{name}}",
                $"{baseRoute}/{{id}}",
                $"{baseRoute}/sync",
                $"{baseRoute}/sync/by-date",
                [
                    $"{baseRoute}/list",
                    $"{baseRoute}/{{name}}",
                    $"{baseRoute}/{{id}}",
                    $"{baseRoute}/{{name}}/area-recommendation",
                    $"{baseRoute}/sync",
                    $"{baseRoute}/sync/by-date"
                ]);
        }

        private static ApiSnapshotResourceResponse CreateItemResource(SourceState state)
        {
            const string baseRoute = "/api/v1/items";

            return new ApiSnapshotResourceResponse(
                "items",
                "Items",
                state.Count,
                state.LatestUpdatedAtUtc,
                baseRoute,
                $"{baseRoute}/{{name}}",
                $"{baseRoute}/{{id}}",
                $"{baseRoute}/sync",
                $"{baseRoute}/sync/by-date",
                [
                    baseRoute,
                    $"{baseRoute}/list",
                    $"{baseRoute}/categories",
                    $"{baseRoute}/{{name}}",
                    $"{baseRoute}/{{id}}",
                    $"{baseRoute}/categories/{{category}}",
                    $"{baseRoute}/sync",
                    $"{baseRoute}/sync/by-date"
                ]);
        }

        private static ApiSnapshotResourceResponse CreateKeyResource(SourceState state)
        {
            const string baseRoute = "/api/v1/keys";

            return new ApiSnapshotResourceResponse(
                "keys",
                "Keys",
                state.Count,
                state.LatestUpdatedAtUtc,
                $"{baseRoute}/list",
                $"{baseRoute}/{{name}}",
                $"{baseRoute}/{{id}}",
                $"{baseRoute}/sync",
                $"{baseRoute}/sync/by-date",
                [
                    $"{baseRoute}/list",
                    $"{baseRoute}/{{name}}",
                    $"{baseRoute}/{{id}}",
                    $"{baseRoute}/sync",
                    $"{baseRoute}/sync/by-date"
                ]);
        }

        private static ApiSnapshotResourceResponse CreateCategoryResource(SourceState state)
        {
            const string baseRoute = "/api/v1/categories";

            return new ApiSnapshotResourceResponse(
                "categories",
                "Categories",
                state.Count,
                state.LatestUpdatedAtUtc,
                $"{baseRoute}/list",
                $"{baseRoute}/{{slug}}",
                $"{baseRoute}/{{id}}",
                null,
                null,
                [
                    $"{baseRoute}/list",
                    $"{baseRoute}/{{slug}}",
                    $"{baseRoute}/{{id}}"
                ]);
        }

        private static ApiSnapshotResourceResponse CreateAssetResource(SourceState state)
        {
            const string baseRoute = "/api/v1/assets";

            return new ApiSnapshotResourceResponse(
                "assets",
                "Assets",
                state.Count,
                state.LatestUpdatedAtUtc,
                null,
                null,
                $"{baseRoute}/{{id}}",
                null,
                null,
                [
                    $"{baseRoute}/{{id}}",
                    $"{baseRoute}/metadata/{{id}}"
                ]);
        }

        private sealed record SourceState(int Count, DateTime? LatestUpdatedAtUtc)
        {
            public static readonly SourceState Empty = new(0, null);
        }

        private sealed record ItemDeltaRow(int Id, string Name, DateTime LastUpdated, bool IsMissingFromSource);

        private sealed record CreatureDeltaRow(int Id, string Name, DateTime LastUpdated);

        private sealed record CategoryDeltaRow(int Id, string Slug, DateTime UpdatedAt, bool IsDeleted);

        private sealed record AssetDeltaRow(int Id, string StorageKey, DateTime UpdatedAt);

        private sealed record WikiArticleDeltaRow(
            int Id,
            WikiContentType ContentType,
            string Title,
            DateTime LastUpdated,
            bool IsDeleted);

        private sealed record ArticleStateRow(WikiContentType ContentType, int Count, DateTime? LatestUpdatedAtUtc);

        private sealed record AggregateState(
            SourceState ItemState,
            SourceState WikiArticleState,
            SourceState CreatureState,
            SourceState CategoryState,
            SourceState AssetState,
            SourceState KeyState,
            IReadOnlyDictionary<WikiContentType, SourceState> ArticleStates);
    }
}
