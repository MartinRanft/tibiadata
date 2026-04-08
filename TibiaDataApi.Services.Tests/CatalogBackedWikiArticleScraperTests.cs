using System.Net;

using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Scraper.Implementations;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Tests
{
    public sealed class CatalogBackedWikiArticleScraperTests
    {
        [Fact]
        public void BuildArticle_ParsesQuestSummarySectionsLinksAndInfobox()
        {
            const string rawWikiText = """
                                       {{Quest
                                       | name = The Desert Dungeon Quest
                                       | reward = 10,000 experience
                                       | location = Darashia
                                       }}

                                       The Desert Dungeon Quest grants access to several dungeons.

                                       == Requirements ==
                                       Players need to complete a lever puzzle.

                                       == Walkthrough ==
                                       Speak with [[Alesar]] and then enter [[Darashia]].
                                       """;

            const string renderedHtml = """
                                        <div class="mw-parser-output">
                                          <table class="infobox"><tr><td>ignored</td></tr></table>
                                          <p>The Desert Dungeon Quest grants access to several dungeons.</p>
                                          <h2><span class="mw-headline">Requirements</span></h2>
                                          <p>Players need to complete a lever puzzle.</p>
                                          <h2><span class="mw-headline">Walkthrough</span></h2>
                                          <p>Speak with <a href="/wiki/Alesar">Alesar</a> and then enter <a href="/wiki/Darashia">Darashia</a>.</p>
                                        </div>
                                        """;

            TestCatalogBackedWikiArticleScraper scraper = new(WikiContentType.Quest, "quest-overview-pages");

            WikiArticle article = scraper.Parse("The Desert Dungeon Quest", rawWikiText, renderedHtml);

            Assert.Equal(WikiContentType.Quest, article.ContentType);
            Assert.Equal("The Desert Dungeon Quest", article.Title);
            Assert.Equal("the desert dungeon quest", article.NormalizedTitle);
            Assert.Equal("The Desert Dungeon Quest grants access to several dungeons.", article.Summary);
            Assert.Contains("Requirements", article.Sections);
            Assert.Contains("Walkthrough", article.Sections);
            Assert.Contains("Alesar", article.LinkedTitles);
            Assert.Contains("Darashia", article.LinkedTitles);
            Assert.Equal("Quest", article.InfoboxTemplate);
            Assert.NotNull(article.InfoboxJson);
            Assert.Contains("reward", article.InfoboxJson);
            Assert.Contains("location", article.InfoboxJson);
            Assert.NotNull(article.PlainTextContent);
            Assert.Contains("lever puzzle", article.PlainTextContent);
        }

        [Fact]
        public void BuildArticle_ParsesHuntingPlaceContent()
        {
            const string rawWikiText = """
                                       {{Hunting Place
                                       | name = Fenrock DL Seal
                                       | recommendedlevel = 180+
                                       | city = Fenrock
                                       }}

                                       Fenrock DL Seal is a dangerous hunting place for experienced players.

                                       == Creatures ==
                                       [[Dragon Lord]], [[Frost Dragon]]
                                       """;

            const string renderedHtml = """
                                        <div class="mw-parser-output">
                                          <p>Fenrock DL Seal is a dangerous hunting place for experienced players.</p>
                                          <h2><span class="mw-headline">Creatures</span></h2>
                                          <ul>
                                            <li><a href="/wiki/Dragon_Lord">Dragon Lord</a></li>
                                            <li><a href="/wiki/Frost_Dragon">Frost Dragon</a></li>
                                          </ul>
                                        </div>
                                        """;

            TestCatalogBackedWikiArticleScraper scraper = new(WikiContentType.HuntingPlace, "hunting-places");

            WikiArticle article = scraper.Parse("Fenrock DL Seal", rawWikiText, renderedHtml);

            Assert.Equal(WikiContentType.HuntingPlace, article.ContentType);
            Assert.Equal("Fenrock DL Seal", article.Title);
            Assert.Equal("fenrock dl seal", article.NormalizedTitle);
            Assert.Equal("Fenrock DL Seal is a dangerous hunting place for experienced players.", article.Summary);
            Assert.Contains("Creatures", article.Sections);
            Assert.Contains("Dragon Lord", article.LinkedTitles);
            Assert.Contains("Frost Dragon", article.LinkedTitles);
            Assert.Equal("Hunting Place", article.InfoboxTemplate);
        }

        [Fact]
        public void BuildArticle_StripsLootStatisticsNamespaceFromStoredTitle()
        {
            const string rawWikiText = """
                                       {{Loot Statistics
                                       | creature = Abyssador
                                       }}
                                       """;

            const string renderedHtml = """
                                        <div class="mw-parser-output">
                                          <p>Loot statistics for Abyssador.</p>
                                        </div>
                                        """;

            TestCatalogBackedWikiArticleScraper scraper = new(WikiContentType.LootStatistic, "loot-statistics");

            WikiArticle article = scraper.Parse("Loot Statistics:Abyssador", rawWikiText, renderedHtml);

            Assert.Equal("Abyssador", article.Title);
            Assert.Equal("abyssador", article.NormalizedTitle);
            Assert.Contains("Loot_Statistics%3AAbyssador", article.WikiUrl);
        }

        [Fact]
        public async Task GetPagesAsync_KeepsNamespacedLootStatisticsPages()
        {
            const string categoryMembersJson = """
                                               {
                                                 "batchcomplete": "",
                                                 "query": {
                                                   "categorymembers": [
                                                     { "ns": 3000, "title": "Loot Statistics:Abyssador" },
                                                     { "ns": 3000, "title": "Loot Statistics:Adventurer" }
                                                   ]
                                                 }
                                               }
                                               """;

            TestCatalogBackedWikiArticleScraper scraper = new(
                WikiContentType.LootStatistic,
                "loot-statistics",
                new StubTibiaWikiHttpService(categoryMembersJson));

            IReadOnlyList<string> pages = await scraper.GetPagesAsync();

            Assert.Contains("Loot Statistics:Abyssador", pages);
            Assert.Contains("Loot Statistics:Adventurer", pages);
        }

        [Fact]
        public async Task GetPagesAsync_ExcludesSubCategoriesButKeepsMainNamespaceColonTitles()
        {
            const string categoryMembersJson = """
                                               {
                                                 "batchcomplete": "",
                                                 "query": {
                                                   "categorymembers": [
                                                     { "ns": 0, "title": "Objects" },
                                                     { "ns": 0, "title": "The Djinn War: Marid Faction" },
                                                     { "ns": 14, "title": "Category:Decoration" }
                                                   ]
                                                 }
                                               }
                                               """;

            TestCatalogBackedWikiArticleScraper scraper = new(
                WikiContentType.Object,
                "objects",
                new StubTibiaWikiHttpService(categoryMembersJson));

            IReadOnlyList<string> pages = await scraper.GetPagesAsync();

            Assert.Contains("Objects", pages);
            Assert.Contains("The Djinn War: Marid Faction", pages);
            Assert.DoesNotContain("Category:Decoration", pages);
        }

        [Fact]
        public async Task GetPagesAsync_KeepsColonTitlesForAllPagesSource()
        {
            const string allPagesJson = """
                                        {
                                          "batchcomplete": "",
                                          "query": {
                                            "allpages": [
                                              { "title": "A Test Page" },
                                              { "title": "The Djinn War: Marid Faction" }
                                            ]
                                          }
                                        }
                                        """;

            TestCatalogBackedWikiArticleScraper scraper = new(
                WikiContentType.WikiPage,
                "wiki-pages",
                new StubTibiaWikiHttpService(allPagesJson: allPagesJson));

            IReadOnlyList<string> pages = await scraper.GetPagesAsync();

            Assert.Contains("A Test Page", pages);
            Assert.Contains("The Djinn War: Marid Faction", pages);
        }

        [Fact]
        public async Task GetPagesAsync_AllPagesPreservesCaseSensitiveTitles()
        {
            const string allPagesJson = """
                                        {
                                          "batchcomplete": "",
                                          "query": {
                                            "allpages": [
                                              { "title": "Alpha" },
                                              { "title": "alpha" }
                                            ]
                                          }
                                        }
                                        """;

            TestCatalogBackedWikiArticleScraper scraper = new(
                WikiContentType.WikiPage,
                "wiki-pages",
                new StubTibiaWikiHttpService(allPagesJson: allPagesJson));

            IReadOnlyList<string> pages = await scraper.GetPagesAsync();

            Assert.Contains("Alpha", pages);
            Assert.Contains("alpha", pages);
            Assert.Equal(2, pages.Count);
        }

        [Fact]
        public void BuildArticle_WikiPagesUseCaseSensitiveIdentityKey()
        {
            const string rawWikiText = """
                                       Plain wiki page content.
                                       """;

            const string renderedHtml = """
                                        <div class="mw-parser-output">
                                          <p>Plain wiki page content.</p>
                                        </div>
                                        """;

            TestCatalogBackedWikiArticleScraper scraper = new(WikiContentType.WikiPage, "wiki-pages");

            WikiArticle upper = scraper.Parse("Alpha", rawWikiText, renderedHtml);
            WikiArticle lower = scraper.Parse("alpha", rawWikiText, renderedHtml);

            Assert.NotEqual(upper.NormalizedTitle, lower.NormalizedTitle);
            Assert.StartsWith("wiki-page:", upper.NormalizedTitle, StringComparison.Ordinal);
            Assert.StartsWith("wiki-page:", lower.NormalizedTitle, StringComparison.Ordinal);
        }

        [Fact]
        public void BuildArticle_PreservesOverlongInfoboxTemplateName()
        {
            string templateName = new('A', 260);
            string rawWikiText = $"{{{{{templateName}\n| key = value\n}}}}";

            const string renderedHtml = """
                                        <div class="mw-parser-output">
                                          <p>Template length test.</p>
                                        </div>
                                        """;

            TestCatalogBackedWikiArticleScraper scraper = new(WikiContentType.WikiPage, "wiki-pages");

            WikiArticle article = scraper.Parse("Template Length Test", rawWikiText, renderedHtml);

            Assert.NotNull(article.InfoboxTemplate);
            Assert.Equal(260, article.InfoboxTemplate!.Length);
            Assert.Equal(templateName, article.InfoboxTemplate);
        }

        [Fact]
        public async Task FetchArticleAsync_WikiPageFallsBackToRenderedHtmlWhenRawWikiTextIsEmpty()
        {
            const string rawWikiTextJson = """
                                           {
                                             "query": {
                                               "pages": {
                                                 "110057": {
                                                   "pageid": 110057,
                                                   "ns": 0,
                                                   "title": "Tamed Frazzlemaw",
                                                   "revisions": [
                                                     {
                                                       "*": ""
                                                     }
                                                   ]
                                                 }
                                               }
                                             }
                                           }
                                           """;

            const string renderedHtmlJson = """
                                            {
                                              "parse": {
                                                "title": "Tamed Frazzlemaw",
                                                "text": "<div class=\"mw-parser-output\"><p>Tamed Frazzlemaw page.</p></div>"
                                              }
                                            }
                                            """;

            ConfigurableTibiaWikiHttpService httpService = new(requestUri =>
            {
                if(requestUri.Contains("prop=revisions", StringComparison.OrdinalIgnoreCase))
                {
                    return rawWikiTextJson;
                }

                if(requestUri.Contains("action=parse", StringComparison.OrdinalIgnoreCase))
                {
                    return renderedHtmlJson;
                }

                return string.Empty;
            });

            TestCatalogBackedWikiArticleScraper scraper = new(
                WikiContentType.WikiPage,
                "wiki-pages",
                httpService);

            (string? RawWikiText, WikiArticle? Article, Exception? Exception) fetched = await scraper.FetchAsync("Tamed Frazzlemaw");

            Assert.Null(fetched.Exception);
            Assert.Equal(string.Empty, fetched.RawWikiText);
            Assert.NotNull(fetched.Article);
            Assert.Equal("Tamed Frazzlemaw", fetched.Article!.Title);
            Assert.Equal(string.Empty, fetched.Article.RawWikiText);
            Assert.Equal("Tamed Frazzlemaw page.", fetched.Article.Summary);
        }

        [Fact]
        public async Task FetchArticleAsync_WikiPageFallsBackToRawWikiTextWhenRenderedHtmlFails()
        {
            const string rawWikiTextJson = """
                                           {
                                             "query": {
                                               "pages": {
                                                 "64178": {
                                                   "pageid": 64178,
                                                   "ns": 0,
                                                   "title": "List of Items (Ordered)",
                                                   "revisions": [
                                                     {
                                                       "*": "== Items ==\n* [[25 Years Backpack]]"
                                                     }
                                                   ]
                                                 }
                                               }
                                             }
                                           }
                                           """;

            ConfigurableTibiaWikiHttpService httpService = new(requestUri =>
            {
                if(requestUri.Contains("prop=revisions", StringComparison.OrdinalIgnoreCase))
                {
                    return rawWikiTextJson;
                }

                if(requestUri.Contains("action=parse", StringComparison.OrdinalIgnoreCase))
                {
                    throw new HttpRequestException("Backend fetch failed.", null, HttpStatusCode.ServiceUnavailable);
                }

                return string.Empty;
            });

            TestCatalogBackedWikiArticleScraper scraper = new(
                WikiContentType.WikiPage,
                "wiki-pages",
                httpService);

            (string? RawWikiText, WikiArticle? Article, Exception? Exception) fetched = await scraper.FetchAsync("List of Items (Ordered)");

            Assert.Null(fetched.Exception);
            Assert.Equal("== Items ==\n* [[25 Years Backpack]]", fetched.RawWikiText);
            Assert.NotNull(fetched.Article);
            Assert.Equal("List of Items (Ordered)", fetched.Article!.Title);
            Assert.Equal("== Items ==\n* [[25 Years Backpack]]", fetched.Article.RawWikiText);
        }

        private sealed class TestCatalogBackedWikiArticleScraper(
            WikiContentType contentType,
            string categorySlug,
            ITibiaWikiHttpService? tibiaWikiHttpService = null)
        : CatalogBackedWikiArticleScraper(
            contentType,
            categorySlug,
            tibiaWikiHttpService ?? new StubTibiaWikiHttpService(),
            NullLogger.Instance)
        {
            public WikiArticle Parse(string title, string rawWikiText, string renderedHtml)
            {
                return BuildArticle(title, rawWikiText, renderedHtml);
            }

            public Task<IReadOnlyList<string>> GetPagesAsync()
            {
                return GetPagesInCategoryAsync();
            }

            public async Task<(string? RawWikiText, WikiArticle? Article, Exception? Exception)> FetchAsync(
                string title,
                CancellationToken cancellationToken = default)
            {
                FetchedArticleResult fetched = await FetchArticleAsync(title, cancellationToken);
                return (fetched.RawWikiText, fetched.Article, fetched.Exception);
            }
        }

        private sealed class StubTibiaWikiHttpService(
            string? categoryMembersJson = null,
            string? allPagesJson = null) : ITibiaWikiHttpService
        {
            public Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                if(requestUri.Contains("list=categorymembers", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(categoryMembersJson ?? string.Empty);
                }

                if(requestUri.Contains("list=allpages", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(allPagesJson ?? string.Empty);
                }

                return Task.FromResult(string.Empty);
            }

            public Task<byte[]> GetBytesAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Array.Empty<byte>());
            }
        }

        private sealed class ConfigurableTibiaWikiHttpService(
            Func<string, string> responseFactory) : ITibiaWikiHttpService
        {
            public Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(responseFactory(requestUri));
            }

            public Task<byte[]> GetBytesAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Array.Empty<byte>());
            }
        }
    }
}