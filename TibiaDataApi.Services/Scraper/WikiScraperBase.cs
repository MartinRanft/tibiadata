using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.Concurrency;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Scraper
{
    public abstract partial class WikiScraperBase(
        ITibiaWikiHttpService tibiaWikiHttpService,
        ILogger logger) : IWikiScraper
    {
        private static readonly Regex HtmlTableRowLinkRegex = new(
            @"<tr\b[^>]*>\s*<td\b[^>]*>\s*<a href=""/wiki/(?<target>[^""#?]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HtmlAnchorRegex = new(
            @"<a href=""/wiki/(?<target>[^""#?]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();
        protected readonly ILogger Logger = logger;

        protected readonly ITibiaWikiHttpService TibiaWikiHttpService = tibiaWikiHttpService;

        protected abstract string CategorySlug { get; }

        protected virtual WikiContentType ContentType => WikiContentType.Item;

        protected virtual string ScraperName => GetType().Name;

        protected WikiCategoryDefinition CategoryDefinition =>
        TibiaWikiCategoryCatalog.GetRequiredDefinition(ContentType, CategorySlug);

        public string RuntimeScraperName => ScraperName;

        public string RuntimeCategorySlug => CategorySlug;

        public string RuntimeCategoryName => CategoryDefinition.Name;

        public abstract Task ExecuteAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            CancellationToken cancellationToken = default);

        protected async Task<WikiCategory> EnsureCategoryAsync(TibiaDbContext db, CancellationToken cancellationToken)
        {
            using IDisposable categoryLock = await AsyncKeyedLockProvider.AcquireAsync(
                "wiki-category",
                CategorySlug,
                cancellationToken).ConfigureAwait(false);

            WikiCategory? category = await db.WikiCategories
                                             .FirstOrDefaultAsync(entry => entry.Slug == CategorySlug, cancellationToken);

            if(category is not null)
            {
                category.Name = CategoryDefinition.Name;
                category.ContentType = CategoryDefinition.ContentType;
                category.GroupSlug = CategoryDefinition.GroupSlug;
                category.GroupName = CategoryDefinition.GroupName;
                category.SourceKind = CategoryDefinition.SourceKind;
                category.SourceTitle = CategoryDefinition.SourceTitle;
                category.SourceSection = CategoryDefinition.SourceSection;
                category.ObjectClass = CategoryDefinition.ObjectClass;
                category.SortOrder = CategoryDefinition.SortOrder;
                category.IsActive = CategoryDefinition.IsActive;
                category.UpdatedAt = DateTime.UtcNow;
                return category;
            }

            category = new WikiCategory
            {
                Slug = CategorySlug,
                Name = CategoryDefinition.Name,
                ContentType = CategoryDefinition.ContentType,
                GroupSlug = CategoryDefinition.GroupSlug,
                GroupName = CategoryDefinition.GroupName,
                SourceKind = CategoryDefinition.SourceKind,
                SourceTitle = CategoryDefinition.SourceTitle,
                SourceSection = CategoryDefinition.SourceSection,
                ObjectClass = CategoryDefinition.ObjectClass,
                SortOrder = CategoryDefinition.SortOrder,
                IsActive = CategoryDefinition.IsActive,
                UpdatedAt = DateTime.UtcNow
            };

            db.WikiCategories.Add(category);
            await db.SaveChangesAsync(cancellationToken);

            return category;
        }

        protected async Task<IReadOnlyList<string>> GetPagesInCategoryAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return CategoryDefinition.SourceKind switch
            {
                WikiCategorySourceKind.CategoryMembers => await GetCategoryMembersAsync(
                    CategoryDefinition.SourceTitle,
                    false,
                    cancellationToken),
                WikiCategorySourceKind.CategoryMembersWithNamespace => await GetCategoryMembersAsync(
                    CategoryDefinition.SourceTitle,
                    true,
                    cancellationToken),
                WikiCategorySourceKind.WikiPage => await GetLinkedPagesFromWikiPageAsync(
                    CategoryDefinition.SourceTitle,
                    null,
                    cancellationToken),
                WikiCategorySourceKind.WikiPageSection => await GetLinkedPagesFromWikiPageAsync(
                    CategoryDefinition.SourceTitle,
                    CategoryDefinition.SourceSection,
                    cancellationToken),
                WikiCategorySourceKind.AllPages => await GetAllPagesAsync(cancellationToken),
                _ => []
            };
        }

        protected async Task<string> GetWikiTextAsync(string title, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string url = $"api.php?action=query&prop=revisions&rvprop=content&titles={Uri.EscapeDataString(title)}&format=json";

            string json = await TibiaWikiHttpService.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            JsonNode? node = JsonNode.Parse(json);
            JsonObject? pages = node?["query"]?["pages"]?.AsObject();

            if(pages is null || !pages.Any())
            {
                return string.Empty;
            }

            JsonNode? firstPage = pages.First().Value;
            return firstPage?["revisions"]?[0]?["*"]?.ToString() ?? string.Empty;
        }

        protected async Task<string> GetRenderedWikiHtmlAsync(
            string pageTitle,
            string? sectionTitle,
            CancellationToken cancellationToken)
        {
            string escapedTitle = Uri.EscapeDataString(pageTitle);

            if(string.IsNullOrWhiteSpace(sectionTitle))
            {
                string htmlUrl = $"api.php?action=parse&page={escapedTitle}&prop=text&format=json";
                string htmlJson = await TibiaWikiHttpService.GetStringAsync(htmlUrl, cancellationToken).ConfigureAwait(false);
                JsonNode? htmlNode = JsonNode.Parse(htmlJson);
                return htmlNode?["parse"]?["text"]?.ToString() ?? string.Empty;
            }

            string sectionsUrl = $"api.php?action=parse&page={escapedTitle}&prop=sections&format=json";
            string sectionsJson = await TibiaWikiHttpService.GetStringAsync(sectionsUrl, cancellationToken).ConfigureAwait(false);
            JsonNode? sectionsNode = JsonNode.Parse(sectionsJson);
            JsonArray? sections = sectionsNode?["parse"]?["sections"]?.AsArray();

            if(sections is null)
            {
                return string.Empty;
            }

            string? sectionIndex = sections
                                   .Select(section => new
                                   {
                                       Index = section?["index"]?.ToString(),
                                       Line = section?["line"]?.ToString()
                                   })
                                   .FirstOrDefault(section =>
                                   string.Equals(section.Line, sectionTitle, StringComparison.OrdinalIgnoreCase))?.Index;

            if(string.IsNullOrWhiteSpace(sectionIndex))
            {
                return string.Empty;
            }

            string htmlUrlWithSection =
            $"api.php?action=parse&page={escapedTitle}&prop=text&section={Uri.EscapeDataString(sectionIndex)}&format=json";
            string htmlJsonWithSection = await TibiaWikiHttpService.GetStringAsync(htmlUrlWithSection, cancellationToken).ConfigureAwait(false);
            JsonNode? htmlNodeWithSection = JsonNode.Parse(htmlJsonWithSection);

            return htmlNodeWithSection?["parse"]?["text"]?.ToString() ?? string.Empty;
        }

        protected string Extract(string content, params string[] aliases)
        {
            foreach(string key in aliases)
            {
                string pattern = @"\|\s*" + Regex.Escape(key) + @"\s*=[^\S\r\n]*(.*?)(?=(\r?\n\s*\||\}\}))";
                Regex regex = GetFieldRegex(pattern);
                Match match = regex.Match(content);

                if(match.Success)
                {
                    return CleanValue(match.Groups[1].Value);
                }
            }

            return string.Empty;
        }

        protected List<string> ExtractTemplateList(string content, string templateName)
        {
            string pattern = string.Format("\\{{\\{{{0}\\|(.*?)\\}}\\}}", Regex.Escape(templateName));
            Regex regex = GetFieldRegex(pattern);

            Match match = regex.Match(content);
            if(!match.Success)
            {
                return [];
            }

            string rawList = match.Groups[1].Value;

            return rawList.Split('|')
                          .Select(CleanValue)
                          .Where(value => !string.IsNullOrEmpty(value))
                          .ToList();
        }

        protected List<string> ExtractList(string content, params string[] aliases)
        {
            string raw = Extract(content, aliases);
            if(string.IsNullOrWhiteSpace(raw))
            {
                return [];
            }

            return raw.Split(',')
                      .Select(CleanValue)
                      .Where(value => !string.IsNullOrEmpty(value))
                      .ToList();
        }

        protected string CleanValue(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string cleaned = LinkCleanupRegex().Replace(value, "$2");
            cleaned = cleaned.Replace("[[", "").Replace("]]", "");
            cleaned = HtmlCleanupRegex().Replace(cleaned, " ");

            return WebUtility.HtmlDecode(cleaned.Replace("&nbsp;", " ")).Trim();
        }

        protected static string NormalizeWikiTitle(string title)
        {
            return WebUtility.HtmlDecode(Uri.UnescapeDataString(title)
                                            .Replace('_', ' ')
                                            .Trim());
        }

        private static Regex GetFieldRegex(string pattern)
        {
            return RegexCache.GetOrAdd(
                pattern,
                compiledPattern => new Regex(
                    compiledPattern,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled));
        }

        private async Task<IReadOnlyList<string>> GetCategoryMembersAsync(
            string categoryTitle,
            bool includeNamespacedTitles,
            CancellationToken cancellationToken)
        {
            List<(string Title, int Namespace)> titles = new();
            string? continueToken = null;

            do
            {
                string url = $"api.php?action=query&list=categorymembers&cmtype=page&cmtitle={Uri.EscapeDataString(categoryTitle)}&cmlimit=500&format=json";

                if(!includeNamespacedTitles)
                {
                    url += "&cmnamespace=0";
                }

                if(!string.IsNullOrWhiteSpace(continueToken))
                {
                    url += $"&cmcontinue={Uri.EscapeDataString(continueToken)}";
                }

                string json = await TibiaWikiHttpService.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
                JsonNode? node = JsonNode.Parse(json);
                JsonArray? members = node?["query"]?["categorymembers"]?.AsArray();

                if(members is not null)
                {
                    titles.AddRange(members
                                    .Select(member => new
                                    {
                                        Title = member?["title"]?.ToString(),
                                        Namespace = member?["ns"]?.GetValue<int>() ?? 0
                                    })
                                    .Where(member => !string.IsNullOrWhiteSpace(member.Title))
                                    .Select(member => (member.Title!, member.Namespace)));
                }

                continueToken = node?["continue"]?["cmcontinue"]?.ToString();
            } while (!string.IsNullOrWhiteSpace(continueToken));

            IEnumerable<(string Title, int Namespace)> filteredTitles = titles;

            if(includeNamespacedTitles)
            {
                filteredTitles = filteredTitles.Where(entry => entry.Namespace != 14);
            }
            else
            {
                filteredTitles = filteredTitles.Where(entry => entry.Namespace == 0);
            }

            return filteredTitles
                   .Select(entry => entry.Title)
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToList();
        }

        private async Task<IReadOnlyList<string>> GetLinkedPagesFromWikiPageAsync(
            string pageTitle,
            string? sectionTitle,
            CancellationToken cancellationToken)
        {
            string html = await GetRenderedWikiHtmlAsync(pageTitle, sectionTitle, cancellationToken);

            if(string.IsNullOrWhiteSpace(html))
            {
                return [];
            }

            List<string> tableEntries = HtmlTableRowLinkRegex.Matches(html)
                                                             .Select(match => NormalizeWikiTitle(match.Groups["target"].Value))
                                                             .Where(title => !string.IsNullOrWhiteSpace(title))
                                                             .Distinct(StringComparer.OrdinalIgnoreCase)
                                                             .ToList();

            if(tableEntries.Count > 0)
            {
                return tableEntries;
            }

            return HtmlAnchorRegex.Matches(html)
                                  .Select(match => NormalizeWikiTitle(match.Groups["target"].Value))
                                  .Where(title => !string.IsNullOrWhiteSpace(title))
                                  .Where(title => !title.Contains(':', StringComparison.Ordinal))
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .ToList();
        }

        private async Task<IReadOnlyList<string>> GetAllPagesAsync(CancellationToken cancellationToken)
        {
            List<string> titles = new();
            string? continueToken = null;

            do
            {
                string url = "api.php?action=query&list=allpages&apnamespace=0&aplimit=500&format=json";

                if(!string.IsNullOrWhiteSpace(continueToken))
                {
                    url += $"&apcontinue={Uri.EscapeDataString(continueToken)}";
                }

                string json = await TibiaWikiHttpService.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
                JsonNode? node = JsonNode.Parse(json);
                JsonArray? pages = node?["query"]?["allpages"]?.AsArray();

                if(pages is not null)
                {
                    titles.AddRange(pages
                                    .Select(page => page?["title"]?.ToString())
                                    .Where(title => !string.IsNullOrWhiteSpace(title))
                                    .Select(title => title!));
                }

                continueToken = node?["continue"]?["apcontinue"]?.ToString();
            } while (!string.IsNullOrWhiteSpace(continueToken));

            return titles
                   .Distinct(StringComparer.Ordinal)
                   .ToList();
        }

        [GeneratedRegex(@"\[\[([^|\]]+\|)?([^|\]]+)\]\]", RegexOptions.Compiled)]
        private static partial Regex LinkCleanupRegex();

        [GeneratedRegex(@"<.*?>", RegexOptions.Compiled)]
        private static partial Regex HtmlCleanupRegex();
    }
}
