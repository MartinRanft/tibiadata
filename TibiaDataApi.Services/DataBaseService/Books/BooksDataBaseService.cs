using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using TibiaDataApi.Contracts.Public.Books;
using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Books.Interfaces;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.DataBaseService.Books
{
    public sealed class BooksDataBaseService(
        TibiaDbContext db,
        HybridCache hybridCache,
        CachingOptions cachingOptions) : IBooksDataBaseService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly SyncStateResponse EmptySyncState = new(0, DateTime.MinValue, null);

        private readonly HybridCacheEntryOptions _cacheOptions = new()
        {
            Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
            LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
        };

        public async Task<List<BookListItemResponse>> GetBooksAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "books:list",
                async ct =>
                {
                    List<WikiArticle> books = await db.WikiArticles
                                                      .AsNoTracking()
                                                      .Where(x => x.ContentType == WikiContentType.BookText)
                                                      .Where(x => !x.IsMissingFromSource)
                                                      .OrderBy(x => x.Title)
                                                      .ToListAsync(ct);

                    return books.Select(book =>
                                {
                                    Dictionary<string, string>? infobox = ParseBookInfobox(book.InfoboxJson);
                                    string name = GetInfoboxValue(infobox, "title")
                                                  ?? book.Title;
                                    string? summary = BuildSummary(infobox, book.Summary);

                                    return new BookListItemResponse(
                                        book.Id,
                                        name,
                                        summary,
                                        book.WikiUrl,
                                        book.LastUpdated
                                    );
                                })
                                .ToList();
                },
                _cacheOptions,
                [CacheTags.Books],
                cancellationToken);
        }

        public async Task<BookDetailsResponse?> GetBookDetailsByNameAsync(string bookName, CancellationToken cancellationToken = default)
        {
            string normalizedName = EntityNameNormalizer.Normalize(bookName);

            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            return await hybridCache.GetOrCreateAsync(
                $"books:detail:{normalizedName}",
                async ct =>
                {
                    int bookId = await db.WikiArticles
                                         .AsNoTracking()
                                         .Where(x => x.ContentType == WikiContentType.BookText)
                                         .Where(x => !x.IsMissingFromSource)
                                         .Where(x => x.NormalizedTitle == normalizedName)
                                         .OrderBy(x => x.Title)
                                         .Select(x => x.Id)
                                         .FirstOrDefaultAsync(ct);

                    return bookId <= 0 ? null : await GetBookDetailsByIdAsync(bookId, ct);
                },
                _cacheOptions,
                [CacheTags.Books],
                cancellationToken);
        }

        public async Task<BookDetailsResponse?> GetBookDetailsByIdAsync(int bookId, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"books:detail:{bookId}",
                async ct =>
                {
                    if(bookId <= 0)
                    {
                        return null;
                    }

                    WikiArticle? book = await db.WikiArticles
                                                .AsNoTracking()
                                                .Where(x => x.ContentType == WikiContentType.BookText)
                                                .Where(x => !x.IsMissingFromSource)
                                                .Where(x => x.Id == bookId)
                                                .OrderBy(x => x.Title)
                                                .FirstOrDefaultAsync(ct);

                    if(book is null)
                    {
                        return null;
                    }

                    Dictionary<string, string>? infobox = ParseBookInfobox(book.InfoboxJson);
                    string name = GetInfoboxValue(infobox, "title")
                                  ?? book.Title;
                    string? summary = BuildSummary(infobox, book.Summary);
                    BookStructuredDataResponse structuredData = new(
                        book.InfoboxTemplate,
                        MapBookInfobox(infobox),
                        BuildBookPages(infobox));

                    return new BookDetailsResponse(
                        book.Id,
                        name,
                        summary,
                        book.PlainTextContent,
                        book.RawWikiText,
                        structuredData,
                        book.WikiUrl,
                        book.LastSeenAt,
                        book.LastUpdated);
                },
                _cacheOptions,
                [CacheTags.Books],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetBookSyncStatesAsync(CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                "books:sync-state",
                async ct =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.BookText)
                                   .Where(x => !x.IsMissingFromSource)
                                   .OrderByDescending(x => x.LastUpdated)
                                   .ThenByDescending(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       x.LastSeenAt))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Books],
                cancellationToken);
        }

        public async Task<List<SyncStateResponse>> GetBookSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default)
        {
            return await hybridCache.GetOrCreateAsync(
                $"books:sync-state:{time:O}",
                async ct =>
                {
                    return await db.WikiArticles
                                   .AsNoTracking()
                                   .Where(x => x.ContentType == WikiContentType.BookText)
                                   .Where(x => !x.IsMissingFromSource)
                                   .Where(x => x.LastUpdated >= time)
                                   .OrderByDescending(x => x.LastUpdated)
                                   .ThenByDescending(x => x.Id)
                                   .Select(x => new SyncStateResponse(
                                       x.Id,
                                       x.LastUpdated,
                                       x.LastSeenAt))
                                   .ToListAsync(ct);
                },
                _cacheOptions,
                [CacheTags.Books],
                cancellationToken);
        }

        private Dictionary<string, string>? ParseBookInfobox(string? infoboxJson)
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

        private static BookInfoboxResponse? MapBookInfobox(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return null;
            }

            return new BookInfoboxResponse(
                GetInfoboxValue(infobox, "booktype"),
                GetInfoboxValue(infobox, "booktype2"),
                GetInfoboxValue(infobox, "title"),
                GetInfoboxValue(infobox, "pagename"),
                GetInfoboxValue(infobox, "location"),
                GetInfoboxValue(infobox, "blurb"),
                GetInfoboxValue(infobox, "author"),
                GetInfoboxValue(infobox, "returnpage"),
                GetInfoboxValue(infobox, "returnpage2"),
                GetInfoboxValue(infobox, "prevbook"),
                GetInfoboxValue(infobox, "nextbook"),
                GetInfoboxValue(infobox, "relatedpages"),
                GetInfoboxValue(infobox, "text"),
                GetInfoboxValue(infobox, "implemented"),
                infobox);
        }

        private static IReadOnlyList<BookPageResponse> BuildBookPages(IReadOnlyDictionary<string, string>? infobox)
        {
            if(infobox is null || infobox.Count == 0)
            {
                return [];
            }

            List<BookPageResponse> pages = [];

            for(int index = 1; index <= 16; index++)
            {
                string suffix = index == 1 ? string.Empty : index.ToString();
                string? text = NormalizeBookText(GetInfoboxValue(infobox, $"text{suffix}"));

                if(string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                pages.Add(new BookPageResponse(
                    index,
                    text,
                    GetInfoboxValue(infobox, $"returnpage{suffix}"),
                    GetInfoboxValue(infobox, $"booktype{suffix}") ?? GetInfoboxValue(infobox, "booktype"),
                    GetInfoboxValue(infobox, $"location{suffix}") ?? GetInfoboxValue(infobox, "location")));
            }

            return pages;
        }

        private static string? GetInfoboxValue(IReadOnlyDictionary<string, string>? infobox, string key)
        {
            if(infobox is null ||
               !infobox.TryGetValue(key, out string? value) ||
               string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value;
        }

        private static string? BuildSummary(IReadOnlyDictionary<string, string>? infobox, string? storedSummary)
        {
            string? preferredSummary = NormalizeSummary(GetInfoboxValue(infobox, "blurb"));
            if(!string.IsNullOrWhiteSpace(preferredSummary))
            {
                return preferredSummary;
            }

            preferredSummary = NormalizeSummary(storedSummary);
            if(!string.IsNullOrWhiteSpace(preferredSummary))
            {
                return preferredSummary;
            }

            return NormalizeSummary(GetInfoboxValue(infobox, "text"), 280);
        }

        private static string? NormalizeSummary(string? value, int maxLength = 180)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Replace("<br>", " ", StringComparison.OrdinalIgnoreCase)
                                     .Replace("<br/>", " ", StringComparison.OrdinalIgnoreCase)
                                     .Replace("<br />", " ", StringComparison.OrdinalIgnoreCase)
                                     .Replace("<pre>", " ", StringComparison.OrdinalIgnoreCase)
                                     .Replace("</pre>", " ", StringComparison.OrdinalIgnoreCase)
                                     .Trim();

            if(normalized is "?" or "|" || normalized.StartsWith("|", StringComparison.Ordinal))
            {
                return null;
            }

            normalized = Regex.Replace(normalized, "\\s+", " ").Trim();

            if(normalized.Length <= maxLength)
            {
                return normalized;
            }

            return $"{normalized[..(maxLength - 3)].TrimEnd()}...";
        }

        private static string? NormalizeBookText(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
                                     .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
                                     .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
                                     .Replace("\r", string.Empty, StringComparison.Ordinal)
                                     .Trim();

            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }
    }
}
