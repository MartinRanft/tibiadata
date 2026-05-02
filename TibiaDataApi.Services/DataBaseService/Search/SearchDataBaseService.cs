using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Search;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Search.Interfaces;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Search
{
    public sealed class SearchDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : ISearchDataBaseService
    {
        private static readonly SearchTypeDefinition[] SearchTypes =
        [
            new("achievements", "achievements", SearchSourceKind.WikiArticle, WikiContentType.Achievement),
            new("books", "books", SearchSourceKind.WikiArticle, WikiContentType.BookText),
            new("buildings", "buildings", SearchSourceKind.WikiArticle, WikiContentType.Building),
            new("charms", "charms", SearchSourceKind.WikiArticle, WikiContentType.Charm),
            new("corpses", "corpses", SearchSourceKind.WikiArticle, WikiContentType.Corpse),
            new("creatures", "creatures", SearchSourceKind.Creature),
            new("effects", "effects", SearchSourceKind.WikiArticle, WikiContentType.Effect),
            new("hunting-places", "hunting-places", SearchSourceKind.WikiArticle, WikiContentType.HuntingPlace),
            new("items", "items", SearchSourceKind.Item),
            new("keys", "keys", SearchSourceKind.Key),
            new("locations", "locations", SearchSourceKind.WikiArticle, WikiContentType.Location),
            new("missiles", "missiles", SearchSourceKind.WikiArticle, WikiContentType.Missile),
            new("mounts", "mounts", SearchSourceKind.WikiArticle, WikiContentType.Mount),
            new("npcs", "npcs", SearchSourceKind.WikiArticle, WikiContentType.Npc),
            new("objects", "objects", SearchSourceKind.WikiArticle, WikiContentType.Object),
            new("outfits", "outfits", SearchSourceKind.WikiArticle, WikiContentType.Outfit),
            new("quests", "quests", SearchSourceKind.WikiArticle, WikiContentType.Quest),
            new("spells", "spells", SearchSourceKind.WikiArticle, WikiContentType.Spell),
            new("streets", "streets", SearchSourceKind.WikiArticle, WikiContentType.Street)
        ];

        private static readonly IReadOnlyDictionary<string, SearchTypeDefinition> SearchTypesByName =
            SearchTypes.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyList<string> SupportedTypeNames =
            SearchTypes.Select(x => x.Name)
                       .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                       .ToArray();

        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public IReadOnlyList<string> GetSupportedTypes()
        {
            return SupportedTypeNames;
        }

        public async Task<SearchResponse> SearchAsync(
            string query,
            IReadOnlyList<string>? types = null,
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            string normalizedQuery = EntityNameNormalizer.Normalize(query);

            if(string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return new SearchResponse(query, 0, []);
            }

            int normalizedLimit = Math.Clamp(limit, 1, 50);
            string normalizedTypes = NormalizeTypes(types);
            string cacheKey = $"search:query:{normalizedQuery}:types:{normalizedTypes}:limit:{normalizedLimit}".ToLowerInvariant();

            return await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    SearchTypeDefinition[] selectedTypes = ResolveSelectedTypes(types);
                    int fetchLimit = Math.Clamp(normalizedLimit * 3, normalizedLimit, 100);

                    List<SearchCandidate> candidates = [];
                    int totalCount = 0;

                    if(selectedTypes.Any(x => x.SourceKind == SearchSourceKind.Item))
                    {
                        (int count, List<SearchCandidate> results) = await SearchItemsAsync(normalizedQuery, fetchLimit, ct);
                        totalCount += count;
                        candidates.AddRange(results);
                    }

                    if(selectedTypes.Any(x => x.SourceKind == SearchSourceKind.Key))
                    {
                        (int count, List<SearchCandidate> results) = await SearchKeysAsync(normalizedQuery, fetchLimit, ct);
                        totalCount += count;
                        candidates.AddRange(results);
                    }

                    if(selectedTypes.Any(x => x.SourceKind == SearchSourceKind.Creature))
                    {
                        (int count, List<SearchCandidate> results) = await SearchCreaturesAsync(normalizedQuery, fetchLimit, ct);
                        totalCount += count;
                        candidates.AddRange(results);
                    }

                    SearchTypeDefinition[] articleTypes = selectedTypes.Where(x => x.SourceKind == SearchSourceKind.WikiArticle)
                                                                      .ToArray();

                    if(articleTypes.Length > 0)
                    {
                        (int count, List<SearchCandidate> results) = await SearchWikiArticlesAsync(normalizedQuery, fetchLimit, articleTypes, ct);
                        totalCount += count;
                        candidates.AddRange(results);
                    }

                    IReadOnlyList<SearchResultItemResponse> items = candidates.OrderBy(x => x.Rank)
                                                                             .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase)
                                                                             .ThenBy(x => x.Item.Kind, StringComparer.OrdinalIgnoreCase)
                                                                             .Select(x => x.Item)
                                                                             .Take(normalizedLimit)
                                                                             .ToList();

                    return new SearchResponse(query.Trim(), totalCount, items);
                },
                _cacheOptions,
                CacheTags.ScrapedContentTags,
                cancellationToken);
        }

        private async Task<(int Count, List<SearchCandidate> Results)> SearchItemsAsync(
            string normalizedQuery,
            int fetchLimit,
            CancellationToken cancellationToken)
        {
            IQueryable<Entities.Items.Item> query = db.Items
                                                      .AsNoTracking()
                                                      .Where(x => !x.IsMissingFromSource)
                                                      .Where(x => x.Category == null || x.Category.Slug != "keys")
                                                      .Where(x => x.NormalizedName.Contains(normalizedQuery) ||
                                                                  (x.NormalizedActualName != null &&
                                                                   x.NormalizedActualName.Contains(normalizedQuery)));

            int count = await query.CountAsync(cancellationToken);
            List<Entities.Items.Item> entities = await query.ToListAsync(cancellationToken);
            List<ItemSearchProjection> matches = entities.Select(x => new ItemSearchProjection(
                                                          x.Id,
                                                          x.Name,
                                                          x.ActualName,
                                                          x.NormalizedName,
                                                          x.NormalizedActualName,
                                                          x.Category?.Slug,
                                                          x.Category?.Name,
                                                          x.WikiUrl,
                                                          x.LastUpdated))
                                                      .Take(Math.Clamp(fetchLimit * 4, fetchLimit, 200))
                                                      .ToList();

            return (count, matches.Select(x => new SearchCandidate(
                                      Math.Min(
                                          ComputeRank(x.NormalizedName, normalizedQuery),
                                          ComputeRank(x.NormalizedActualName, normalizedQuery)),
                                      new SearchResultItemResponse(
                                          "items",
                                          x.Id,
                                          x.Name,
                                          BuildItemSubtitle(x.Name, x.ActualName),
                                          x.CategorySlug,
                                          x.CategoryName,
                                          null,
                                          BuildRoute("items", x.Name),
                                          x.WikiUrl,
                                          x.LastUpdated)))
                                  .OrderBy(x => x.Rank)
                                  .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase)
                                  .Take(fetchLimit)
                                  .ToList());
        }

        private async Task<(int Count, List<SearchCandidate> Results)> SearchKeysAsync(
            string normalizedQuery,
            int fetchLimit,
            CancellationToken cancellationToken)
        {
            IQueryable<Entities.Items.Item> query = db.Items
                                                      .AsNoTracking()
                                                      .Where(x => !x.IsMissingFromSource)
                                                      .Where(x => x.Category != null && x.Category.Slug == "keys")
                                                      .Where(x => x.NormalizedName.Contains(normalizedQuery) ||
                                                                  (x.NormalizedActualName != null &&
                                                                   x.NormalizedActualName.Contains(normalizedQuery)));

            int count = await query.CountAsync(cancellationToken);
            List<Entities.Items.Item> entities = await query.ToListAsync(cancellationToken);
            List<ItemSearchProjection> matches = entities.Select(x => new ItemSearchProjection(
                                                          x.Id,
                                                          x.Name,
                                                          x.ActualName,
                                                          x.NormalizedName,
                                                          x.NormalizedActualName,
                                                          "keys",
                                                          "Keys",
                                                          x.WikiUrl,
                                                          x.LastUpdated))
                                                      .Take(Math.Clamp(fetchLimit * 4, fetchLimit, 200))
                                                      .ToList();

            return (count, matches.Select(x => new SearchCandidate(
                                      Math.Min(
                                          ComputeRank(x.NormalizedName, normalizedQuery),
                                          ComputeRank(x.NormalizedActualName, normalizedQuery)),
                                      new SearchResultItemResponse(
                                          "keys",
                                          x.Id,
                                          x.Name,
                                          BuildItemSubtitle(x.Name, x.ActualName),
                                          x.CategorySlug,
                                          x.CategoryName,
                                          null,
                                          BuildRoute("keys", x.Name),
                                          x.WikiUrl,
                                          x.LastUpdated)))
                                  .OrderBy(x => x.Rank)
                                  .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase)
                                  .Take(fetchLimit)
                                  .ToList());
        }

        private async Task<(int Count, List<SearchCandidate> Results)> SearchCreaturesAsync(
            string normalizedQuery,
            int fetchLimit,
            CancellationToken cancellationToken)
        {
            IQueryable<Entities.Creatures.Creature> query = db.Creatures
                                                              .AsNoTracking()
                                                              .Where(x => x.NormalizedName.Contains(normalizedQuery));

            int count = await query.CountAsync(cancellationToken);
            List<Entities.Creatures.Creature> entities = await query.ToListAsync(cancellationToken);
            List<CreatureSearchProjection> matches = entities.Select(x => new CreatureSearchProjection(
                                                                     x.Id,
                                                                     x.Name,
                                                                     x.NormalizedName,
                                                                     x.Hitpoints,
                                                                     x.Experience,
                                                                     x.LastUpdated))
                                                                 .Take(Math.Clamp(fetchLimit * 4, fetchLimit, 200))
                                                                 .ToList();

            return (count, matches.Select(x => new SearchCandidate(
                                      ComputeRank(x.NormalizedName, normalizedQuery),
                                      new SearchResultItemResponse(
                                          "creatures",
                                          x.Id,
                                          x.Name,
                                          null,
                                          null,
                                          null,
                                          $"Hitpoints: {x.Hitpoints}, Experience: {x.Experience}",
                                          BuildRoute("creatures", x.Name),
                                          null,
                                          x.LastUpdated)))
                                  .OrderBy(x => x.Rank)
                                  .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase)
                                  .Take(fetchLimit)
                                  .ToList());
        }

        private async Task<(int Count, List<SearchCandidate> Results)> SearchWikiArticlesAsync(
            string normalizedQuery,
            int fetchLimit,
            IReadOnlyList<SearchTypeDefinition> articleTypes,
            CancellationToken cancellationToken)
        {
            IQueryable<Entities.Content.WikiArticle> query = db.WikiArticles
                                                               .AsNoTracking()
                                                               .Where(x => !x.IsMissingFromSource)
                                                               .Where(x => x.NormalizedTitle.Contains(normalizedQuery));

            List<Entities.Content.WikiArticle> entities = await query.ToListAsync(cancellationToken);
            HashSet<WikiContentType> selectedContentTypes = articleTypes.Where(x => x.ContentType is not null)
                                                                        .Select(x => x.ContentType!.Value)
                                                                        .ToHashSet();

            List<Entities.Content.WikiArticle> filteredEntities = entities.Where(x => selectedContentTypes.Contains(x.ContentType))
                                                                          .ToList();

            int count = filteredEntities.Count;
            List<WikiArticleSearchProjection> matches = filteredEntities.Select(x => new WikiArticleSearchProjection(
                                                                         x.Id,
                                                                         x.ContentType,
                                                                         x.Title,
                                                                         x.NormalizedTitle,
                                                                         x.Summary,
                                                                         x.WikiUrl,
                                                                         x.LastUpdated))
                                                                     .Take(Math.Clamp(fetchLimit * 4, fetchLimit, 200))
                                                                     .ToList();

            return (count, matches.Select(x =>
                                  {
                                      SearchTypeDefinition definition = articleTypes.First(type => type.ContentType == x.ContentType);

                                      return new SearchCandidate(
                                          ComputeRank(x.NormalizedTitle, normalizedQuery),
                                          new SearchResultItemResponse(
                                              definition.Name,
                                              x.Id,
                                              x.Title,
                                              null,
                                              null,
                                              null,
                                              x.Summary,
                                              BuildRoute(definition.RouteSegment, x.Title),
                                              x.WikiUrl,
                                              x.LastUpdated));
                                  })
                                  .OrderBy(x => x.Rank)
                                  .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase)
                                  .Take(fetchLimit)
                                  .ToList());
        }

        private static int ComputeRank(string? normalizedValue, string normalizedQuery)
        {
            if(string.IsNullOrWhiteSpace(normalizedValue))
            {
                return 3;
            }

            if(string.Equals(normalizedValue, normalizedQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if(normalizedValue.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return normalizedValue.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ? 2 : 3;
        }

        private static string BuildRoute(string routeSegment, string title)
        {
            return $"/api/v1/{routeSegment}/{Uri.EscapeDataString(title)}";
        }

        private static string NormalizeTypes(IReadOnlyList<string>? types)
        {
            if(types is null || types.Count == 0)
            {
                return "all";
            }

            return string.Join(
                ",",
                types.Where(x => !string.IsNullOrWhiteSpace(x))
                     .Select(x => x.Trim().ToLowerInvariant())
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        private static string? BuildItemSubtitle(string name, string? actualName)
        {
            if(string.IsNullOrWhiteSpace(actualName) ||
               string.Equals(name, actualName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return actualName;
        }

        private static SearchTypeDefinition[] ResolveSelectedTypes(IReadOnlyList<string>? types)
        {
            if(types is null || types.Count == 0)
            {
                return SearchTypes;
            }

            List<SearchTypeDefinition> selected = [];

            foreach(string type in types.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                string normalizedType = type.Trim();

                if(!SearchTypesByName.TryGetValue(normalizedType, out SearchTypeDefinition? definition))
                {
                    continue;
                }

                selected.Add(definition);
            }

            return selected.Count == 0 ? SearchTypes : selected.DistinctBy(x => x.Name).ToArray();
        }

        private enum SearchSourceKind
        {
            Item,
            Key,
            Creature,
            WikiArticle
        }

        private sealed record SearchTypeDefinition(
            string Name,
            string RouteSegment,
            SearchSourceKind SourceKind,
            WikiContentType? ContentType = null);

        private sealed record SearchCandidate(int Rank, SearchResultItemResponse Item);

        private sealed record ItemSearchProjection(
            int Id,
            string Name,
            string? ActualName,
            string NormalizedName,
            string? NormalizedActualName,
            string? CategorySlug,
            string? CategoryName,
            string? WikiUrl,
            DateTime LastUpdated);

        private sealed record CreatureSearchProjection(
            int Id,
            string Name,
            string NormalizedName,
            int Hitpoints,
            long Experience,
            DateTime LastUpdated);

        private sealed record WikiArticleSearchProjection(
            int Id,
            WikiContentType ContentType,
            string Title,
            string NormalizedTitle,
            string? Summary,
            string? WikiUrl,
            DateTime LastUpdated);
    }
}
