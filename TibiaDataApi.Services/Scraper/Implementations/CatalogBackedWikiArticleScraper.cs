using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.Concurrency;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Scraper.Implementations
{
    public partial class CatalogBackedWikiArticleScraper(
        WikiContentType contentType,
        string categorySlug,
        ITibiaWikiHttpService tibiaWikiHttpService,
        ILogger logger) : WikiScraperBase(tibiaWikiHttpService, logger)
    {
        protected override string CategorySlug => categorySlug;

        protected override WikiContentType ContentType => contentType;

        protected override string ScraperName => $"{CategoryDefinition.Name.Replace(" ", string.Empty)}Scraper";

        private bool RequiresImmediatePersistence =>
        TibiaWikiCategoryCatalog.All.Count(entry => entry.ContentType == ContentType) > 1;

        private int FetchBatchSize => RequiresImmediatePersistence ? 4 : 8;

        private int SaveBatchSize => RequiresImmediatePersistence ? 25 : 100;

        public override async Task ExecuteAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WikiCategory category = await EnsureCategoryAsync(db, cancellationToken);
            IReadOnlyList<string> titles = (await GetPagesInCategoryAsync(cancellationToken)).ToList();

            scrapeLog.ScraperName = RuntimeScraperName;
            scrapeLog.CategoryName = CategoryDefinition.Name;
            scrapeLog.CategorySlug = RuntimeCategorySlug;
            scrapeLog.PagesDiscovered = titles.Count;

            HashSet<string> seenTitles = new(StringComparer.Ordinal);
            int pendingWriteCount = 0;

            for (int offset = 0; offset < titles.Count; offset += FetchBatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<string> titleBatch = titles.Skip(offset)
                                                .Take(FetchBatchSize)
                                                .ToList();

                IReadOnlyList<FetchedArticleResult> fetchedBatch = await FetchArticlesAsync(titleBatch, cancellationToken);

                foreach(FetchedArticleResult fetched in fetchedBatch)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if(fetched.Exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    {
                        throw fetched.Exception;
                    }

                    if(fetched.Exception is not null)
                    {
                        RecordFailure(
                            db,
                            scrapeLog,
                            fetched.Title,
                            fetched.Exception.GetType().Name,
                            fetched.Exception.Message,
                            fetched.Exception);
                        pendingWriteCount++;
                    }
                    else if(fetched.Article is not null)
                    {
                        WikiArticleChangeOutcome outcome = await UpsertArticleAsync(
                            db,
                            scrapeLog,
                            fetched.Article,
                            category,
                            cancellationToken);

                        if(!string.IsNullOrWhiteSpace(outcome.NormalizedTitle))
                        {
                            seenTitles.Add(outcome.NormalizedTitle);
                        }

                        pendingWriteCount++;
                    }
                    else if(string.IsNullOrWhiteSpace(fetched.RawWikiText))
                    {
                        RecordFailure(
                            db,
                            scrapeLog,
                            fetched.Title,
                            "NoContent",
                            "No wikitext content was returned for the requested page.",
                            null);
                        pendingWriteCount++;
                    }

                    if(pendingWriteCount >= SaveBatchSize)
                    {
                        await db.SaveChangesAsync(cancellationToken);
                        pendingWriteCount = 0;
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            await MarkMissingFromSourceAsync(db, scrapeLog, category, seenTitles, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            Logger.LogInformation(
                "{ScraperName} finished. Added={Added}, Updated={Updated}, Unchanged={Unchanged}, Missing={Missing}, Failed={Failed}",
                RuntimeScraperName,
                scrapeLog.ItemsAdded,
                scrapeLog.ItemsUpdated,
                scrapeLog.ItemsUnchanged,
                scrapeLog.ItemsMissingFromSource,
                scrapeLog.ItemsFailed);
        }

        protected internal WikiArticle BuildArticle(string title, string rawWikiText, string renderedHtml)
        {
            string storedTitle = GetStoredTitle(title);

            HtmlDocument document = new();
            document.LoadHtml(renderedHtml);

            HtmlNode root = document.DocumentNode.SelectSingleNode("//div[contains(@class,'mw-parser-output')]")
                            ?? document.DocumentNode;

            RemoveNoise(root);

            List<string> sections = ExtractSections(root);
            List<string> linkedTitles = ExtractLinkedTitles(root);
            string? summary = ExtractSummary(root);
            string? plainTextContent = ExtractPlainTextContent(root);
            string? infoboxTemplate = ExtractInfoboxTemplateName(rawWikiText);
            string? infoboxJson = ExtractInfoboxJson(rawWikiText);
            string? additionalAttributesJson = ExtractAdditionalAttributes(rawWikiText, ContentType);

            return new WikiArticle
            {
                ContentType = ContentType,
                Title = storedTitle,
                NormalizedTitle = GetNormalizedStoredTitle(storedTitle),
                Summary = summary,
                PlainTextContent = plainTextContent,
                RawWikiText = rawWikiText,
                InfoboxTemplate = infoboxTemplate,
                InfoboxJson = infoboxJson,
                Sections = sections,
                LinkedTitles = linkedTitles,
                AdditionalAttributesJson = additionalAttributesJson,
                WikiUrl = $"https://tibia.fandom.com/wiki/{Uri.EscapeDataString(title.Replace(" ", "_"))}",
                LastSeenAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsMissingFromSource = false,
                MissingSince = null
            };
        }

        protected internal virtual string GetStoredTitle(string sourceTitle)
        {
            if(ContentType == WikiContentType.LootStatistic &&
               sourceTitle.StartsWith("Loot Statistics:", StringComparison.OrdinalIgnoreCase))
            {
                return sourceTitle["Loot Statistics:".Length..].Trim();
            }

            return sourceTitle;
        }

        protected internal virtual string GetNormalizedStoredTitle(string storedTitle)
        {
            if(ContentType == WikiContentType.WikiPage)
            {
                byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(storedTitle));
                return $"wiki-page:{Convert.ToHexString(hash)}";
            }

            return EntityNameNormalizer.Normalize(storedTitle);
        }

        private async Task<IReadOnlyList<FetchedArticleResult>> FetchArticlesAsync(
            IReadOnlyList<string> titles,
            CancellationToken cancellationToken)
        {
            Task<FetchedArticleResult>[] tasks = titles
                                                 .Select(title => FetchArticleAsync(title, cancellationToken))
                                                 .ToArray();

            return await Task.WhenAll(tasks);
        }

        protected internal virtual async Task<FetchedArticleResult> FetchArticleAsync(
            string title,
            CancellationToken cancellationToken)
        {
            string? rawWikiText = null;
            string? renderedHtml = null;
            Exception? rawWikiTextException = null;
            Exception? renderedHtmlException = null;

            try
            {
                Task<string> rawWikiTextTask = GetWikiTextAsync(title, cancellationToken);
                Task<string> renderedHtmlTask = GetRenderedWikiHtmlAsync(title, null, cancellationToken);

                try
                {
                    rawWikiText = await rawWikiTextTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    return new FetchedArticleResult(title, null, null, ex);
                }
                catch (Exception ex)
                {
                    rawWikiTextException = ex;
                }

                try
                {
                    renderedHtml = await renderedHtmlTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    return new FetchedArticleResult(title, null, null, ex);
                }
                catch (Exception ex)
                {
                    renderedHtmlException = ex;
                }

                if(ContentType == WikiContentType.WikiPage)
                {
                    if(rawWikiTextException is not null && !string.IsNullOrWhiteSpace(renderedHtml))
                    {
                        rawWikiText = string.Empty;
                        rawWikiTextException = null;
                    }

                    if(renderedHtmlException is not null && !string.IsNullOrWhiteSpace(rawWikiText))
                    {
                        renderedHtml = string.Empty;
                        renderedHtmlException = null;
                    }

                    if(string.IsNullOrWhiteSpace(rawWikiText) && !string.IsNullOrWhiteSpace(renderedHtml))
                    {
                        rawWikiText = string.Empty;
                    }
                }

                if(rawWikiTextException is not null)
                {
                    return new FetchedArticleResult(title, null, null, rawWikiTextException);
                }

                if(renderedHtmlException is not null)
                {
                    return new FetchedArticleResult(title, rawWikiText, null, renderedHtmlException);
                }

                if(string.IsNullOrWhiteSpace(rawWikiText) && string.IsNullOrWhiteSpace(renderedHtml))
                {
                    return new FetchedArticleResult(title, rawWikiText, null, null);
                }

                if(string.IsNullOrWhiteSpace(rawWikiText) && ContentType != WikiContentType.WikiPage)
                {
                    return new FetchedArticleResult(title, rawWikiText, null, null);
                }

                WikiArticle article = BuildArticle(title, rawWikiText ?? string.Empty, renderedHtml ?? string.Empty);
                return new FetchedArticleResult(title, rawWikiText, article, null);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                return new FetchedArticleResult(title, null, null, ex);
            }
            catch (Exception ex)
            {
                return new FetchedArticleResult(title, null, null, ex);
            }
        }

        private async Task<WikiArticleChangeOutcome> UpsertArticleAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            WikiArticle article,
            WikiCategory category,
            CancellationToken cancellationToken)
        {
            using IDisposable articleLock = await AsyncKeyedLockProvider.AcquireAsync(
                "wiki-article",
                BuildArticleLockKey(article.ContentType, article.NormalizedTitle),
                cancellationToken).ConfigureAwait(false);

            WikiArticle? existing = await db.WikiArticles
                                            .Include(entry => entry.WikiArticleCategories)
                                            .ThenInclude(entry => entry.WikiCategory)
                                            .FirstOrDefaultAsync(
                                                entry => entry.ContentType == article.ContentType && entry.NormalizedTitle == article.NormalizedTitle,
                                                cancellationToken);

            scrapeLog.PagesProcessed++;
            scrapeLog.ItemsProcessed++;

            if(existing is null)
            {
                WikiArticleCategory addedRelation = new()
                {
                    WikiCategoryId = category.Id,
                    WikiCategory = category,
                    FirstSeenAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                article.WikiArticleCategories.Add(addedRelation);
                db.WikiArticles.Add(article);

                db.ScrapeItemChanges.Add(new ScrapeItemChange
                {
                    ScrapeLogId = scrapeLog.Id,
                    ItemName = article.Title,
                    ChangeType = ScrapeChangeType.Added,
                    CategorySlug = RuntimeCategorySlug,
                    CategoryName = CategoryDefinition.Name,
                    AfterJson = CreateArticleSnapshotJson(article)
                });

                scrapeLog.ItemsAdded++;
                UpdateChangesSummary(scrapeLog);

                if(RequiresImmediatePersistence)
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

                return new WikiArticleChangeOutcome(article.Title, article.NormalizedTitle);
            }

            List<string> changedFields = GetChangedFields(existing, article);
            string beforeJson = CreateArticleSnapshotJson(existing);
            bool wasMissingFromSource = existing.IsMissingFromSource;

            ApplyCurrentValues(existing, article);

            WikiArticleCategory? relation = existing.WikiArticleCategories
                                                    .FirstOrDefault(entry => entry.WikiCategoryId == category.Id);

            if(relation is null)
            {
                existing.WikiArticleCategories.Add(new WikiArticleCategory
                {
                    WikiArticleId = existing.Id,
                    WikiCategoryId = category.Id,
                    WikiCategory = category,
                    FirstSeenAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                changedFields.Add("categories");
            }
            else
            {
                bool relationWasMissing = relation.IsMissingFromSource;

                relation.LastSeenAt = DateTime.UtcNow;
                relation.IsMissingFromSource = false;
                relation.MissingSince = null;
                relation.UpdatedAt = DateTime.UtcNow;

                if(relationWasMissing)
                {
                    changedFields.Add("categoryAvailability");
                }
            }

            existing.LastSeenAt = DateTime.UtcNow;
            existing.IsMissingFromSource = false;
            existing.MissingSince = null;

            if(wasMissingFromSource)
            {
                changedFields.Add(nameof(WikiArticle.IsMissingFromSource));
            }

            if(changedFields.Count == 0)
            {
                scrapeLog.ItemsUnchanged++;
                UpdateChangesSummary(scrapeLog);
                return new WikiArticleChangeOutcome(existing.Title, existing.NormalizedTitle);
            }

            existing.LastUpdated = DateTime.UtcNow;

            db.ScrapeItemChanges.Add(new ScrapeItemChange
            {
                ScrapeLogId = scrapeLog.Id,
                ItemName = existing.Title,
                ChangeType = ScrapeChangeType.Updated,
                CategorySlug = RuntimeCategorySlug,
                CategoryName = CategoryDefinition.Name,
                ChangedFieldsJson = JsonSerializer.Serialize(changedFields.Distinct(StringComparer.OrdinalIgnoreCase)),
                BeforeJson = beforeJson,
                AfterJson = CreateArticleSnapshotJson(existing)
            });

            scrapeLog.ItemsUpdated++;
            UpdateChangesSummary(scrapeLog);

            if(RequiresImmediatePersistence)
            {
                await db.SaveChangesAsync(cancellationToken);
            }

            return new WikiArticleChangeOutcome(existing.Title, existing.NormalizedTitle);
        }

        private async Task MarkMissingFromSourceAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            WikiCategory category,
            IReadOnlySet<string> seenTitles,
            CancellationToken cancellationToken)
        {
            List<WikiArticleCategory> categoryRelations = await db.WikiArticleCategories
                                                                  .Include(entry => entry.WikiArticle)
                                                                  .ThenInclude(article => article!.WikiArticleCategories)
                                                                  .ThenInclude(entry => entry.WikiCategory)
                                                                  .Where(entry => entry.WikiCategoryId == category.Id)
                                                                  .Where(entry => !entry.IsMissingFromSource)
                                                                  .ToListAsync(cancellationToken);

            List<WikiArticleCategory> missingRelations = categoryRelations
                                                         .Where(entry => entry.WikiArticle is not null)
                                                         .Where(entry => !seenTitles.Contains(entry.WikiArticle!.NormalizedTitle))
                                                         .ToList();

            foreach(WikiArticleCategory relation in missingRelations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if(relation.WikiArticle is null)
                {
                    continue;
                }

                using IDisposable articleLock = await AsyncKeyedLockProvider.AcquireAsync(
                    "wiki-article",
                    BuildArticleLockKey(ContentType, relation.WikiArticle.NormalizedTitle),
                    cancellationToken).ConfigureAwait(false);

                WikiArticleCategory? trackedRelation = await db.WikiArticleCategories
                                                               .Include(entry => entry.WikiArticle)
                                                               .ThenInclude(article => article!.WikiArticleCategories)
                                                               .ThenInclude(entry => entry.WikiCategory)
                                                               .FirstOrDefaultAsync(
                                                                   entry => entry.WikiArticleId == relation.WikiArticleId &&
                                                                            entry.WikiCategoryId == category.Id,
                                                                   cancellationToken);

                if(trackedRelation?.WikiArticle is null || trackedRelation.IsMissingFromSource)
                {
                    continue;
                }

                string beforeJson = CreateArticleSnapshotJson(trackedRelation.WikiArticle);

                trackedRelation.IsMissingFromSource = true;
                trackedRelation.MissingSince = DateTime.UtcNow;
                trackedRelation.UpdatedAt = DateTime.UtcNow;

                UpdateArticleMissingState(trackedRelation.WikiArticle);

                db.ScrapeItemChanges.Add(new ScrapeItemChange
                {
                    ScrapeLogId = scrapeLog.Id,
                    ItemName = trackedRelation.WikiArticle.Title,
                    ChangeType = ScrapeChangeType.MissingFromSource,
                    CategorySlug = RuntimeCategorySlug,
                    CategoryName = CategoryDefinition.Name,
                    BeforeJson = beforeJson,
                    AfterJson = CreateArticleSnapshotJson(trackedRelation.WikiArticle)
                });

                scrapeLog.ItemsMissingFromSource++;
                UpdateChangesSummary(scrapeLog);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        private void RecordFailure(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            string title,
            string errorType,
            string message,
            Exception? exception)
        {
            scrapeLog.ItemsFailed++;
            scrapeLog.PagesFailed++;

            db.ScrapeErrors.Add(new ScrapeError
            {
                ScrapeLogId = scrapeLog.Id,
                Scope = "Page",
                PageTitle = title,
                ItemName = title,
                ErrorType = errorType,
                Message = message,
                DetailsJson = exception is null
                ? null
                : JsonSerializer.Serialize(new
                {
                    exception.Message,
                    exception.StackTrace
                })
            });

            db.ScrapeItemChanges.Add(new ScrapeItemChange
            {
                ScrapeLogId = scrapeLog.Id,
                ItemName = title,
                ChangeType = ScrapeChangeType.Failed,
                CategorySlug = RuntimeCategorySlug,
                CategoryName = CategoryDefinition.Name,
                ErrorMessage = message
            });

            UpdateChangesSummary(scrapeLog);
        }

        private static void ApplyCurrentValues(WikiArticle existing, WikiArticle incoming)
        {
            existing.Title = incoming.Title;
            existing.NormalizedTitle = incoming.NormalizedTitle;
            existing.Summary = incoming.Summary;
            existing.PlainTextContent = incoming.PlainTextContent;
            existing.RawWikiText = incoming.RawWikiText;
            existing.InfoboxTemplate = incoming.InfoboxTemplate;
            existing.InfoboxJson = incoming.InfoboxJson;
            existing.Sections = incoming.Sections.ToList();
            existing.LinkedTitles = incoming.LinkedTitles.ToList();
            existing.AdditionalAttributesJson = incoming.AdditionalAttributesJson;
            existing.WikiUrl = incoming.WikiUrl;
        }

        private static List<string> GetChangedFields(WikiArticle existing, WikiArticle incoming)
        {
            List<string> changedFields = [];

            CompareField(changedFields, nameof(WikiArticle.Title), existing.Title, incoming.Title);
            CompareField(changedFields, nameof(WikiArticle.Summary), existing.Summary, incoming.Summary);
            CompareField(changedFields, nameof(WikiArticle.PlainTextContent), existing.PlainTextContent, incoming.PlainTextContent);
            CompareField(changedFields, nameof(WikiArticle.RawWikiText), existing.RawWikiText, incoming.RawWikiText);
            CompareField(changedFields, nameof(WikiArticle.InfoboxTemplate), existing.InfoboxTemplate, incoming.InfoboxTemplate);
            CompareField(changedFields, nameof(WikiArticle.InfoboxJson), existing.InfoboxJson, incoming.InfoboxJson);
            CompareField(changedFields, nameof(WikiArticle.AdditionalAttributesJson), existing.AdditionalAttributesJson, incoming.AdditionalAttributesJson);
            CompareField(changedFields, nameof(WikiArticle.WikiUrl), existing.WikiUrl, incoming.WikiUrl);

            if(!existing.Sections.SequenceEqual(incoming.Sections))
            {
                changedFields.Add(nameof(WikiArticle.Sections));
            }

            if(!existing.LinkedTitles.SequenceEqual(incoming.LinkedTitles))
            {
                changedFields.Add(nameof(WikiArticle.LinkedTitles));
            }

            return changedFields;
        }

        private static void CompareField(ICollection<string> changedFields, string fieldName, string? existing, string? incoming)
        {
            if(!string.Equals(existing, incoming, StringComparison.Ordinal))
            {
                changedFields.Add(fieldName);
            }
        }

        private static void UpdateArticleMissingState(WikiArticle article)
        {
            bool hasVisibleRelation = article.WikiArticleCategories.Any(entry => !entry.IsMissingFromSource);

            article.IsMissingFromSource = !hasVisibleRelation;
            article.MissingSince = hasVisibleRelation ? null : DateTime.UtcNow;
            article.LastUpdated = DateTime.UtcNow;
        }

        private static string CreateArticleSnapshotJson(WikiArticle article)
        {
            string rawContentHash = ComputeHash(article.RawWikiText);
            string plainTextHash = ComputeHash(article.PlainTextContent);
            IEnumerable<string> categorySlugs = article.WikiArticleCategories
                                                       .Select(entry => entry.WikiCategory?.Slug)
                                                       .Where(value => !string.IsNullOrWhiteSpace(value))
                                                       .Select(value => value!)
                                                       .OrderBy(value => value, StringComparer.OrdinalIgnoreCase);

            return JsonSerializer.Serialize(new
            {
                article.ContentType,
                article.Title,
                article.Summary,
                article.InfoboxTemplate,
                article.InfoboxJson,
                article.Sections,
                article.LinkedTitles,
                article.AdditionalAttributesJson,
                article.WikiUrl,
                article.IsMissingFromSource,
                article.MissingSince,
                RawWikiTextHash = rawContentHash,
                PlainTextContentHash = plainTextHash,
                Categories = categorySlugs
            });
        }

        private static string ComputeHash(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }

        private static string BuildArticleLockKey(WikiContentType contentType, string normalizedTitle)
        {
            return $"{contentType}:{normalizedTitle}";
        }

        private static void UpdateChangesSummary(ScrapeLog scrapeLog)
        {
            scrapeLog.ChangesJson = JsonSerializer.Serialize(new
            {
                scrapeLog.ItemsAdded,
                scrapeLog.ItemsUpdated,
                scrapeLog.ItemsUnchanged,
                scrapeLog.ItemsFailed,
                scrapeLog.ItemsMissingFromSource
            });
        }

        private static void RemoveNoise(HtmlNode root)
        {
            HtmlNodeCollection? noiseNodes = root.SelectNodes(
                ".//table|.//style|.//script|.//sup[contains(@class,'reference')]|.//div[@id='toc' or contains(@class,'toc')]");

            if(noiseNodes is null)
            {
                return;
            }

            foreach(HtmlNode node in noiseNodes.ToList())
            {
                node.Remove();
            }
        }

        private static string? ExtractSummary(HtmlNode root)
        {
            List<string> paragraphs = root.SelectNodes(".//p")
                                          ?.Select(node => NormalizeText(node.InnerText))
                                          .Where(value => !string.IsNullOrWhiteSpace(value))
                                          .Where(value => value.Length >= 20)
                                          .ToList()
                                      ?? [];

            return paragraphs.FirstOrDefault();
        }

        private static string? ExtractPlainTextContent(HtmlNode root)
        {
            IEnumerable<string> blocks = root.Descendants()
                                             .Where(node => node.Name is "p" or "li" or "h2" or "h3" or "h4")
                                             .Select(node => NormalizeText(node.InnerText))
                                             .Where(value => !string.IsNullOrWhiteSpace(value));

            string content = string.Join(Environment.NewLine + Environment.NewLine, blocks).Trim();
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }

        private static List<string> ExtractSections(HtmlNode root)
        {
            return root.SelectNodes(".//span[contains(@class,'mw-headline')]")
                       ?.Select(node => NormalizeText(node.InnerText))
                       .Where(value => !string.IsNullOrWhiteSpace(value))
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList()
                   ?? [];
        }

        private static List<string> ExtractLinkedTitles(HtmlNode root)
        {
            return root.SelectNodes(".//a[starts-with(@href, '/wiki/')]")
                       ?.Select(node => node.GetAttributeValue("href", string.Empty))
                       .Where(href => !string.IsNullOrWhiteSpace(href))
                       .Select(href => href.Split('#', '?')[0])
                       .Select(href => href["/wiki/".Length..])
                       .Select(NormalizeWikiTitle)
                       .Where(title => !string.IsNullOrWhiteSpace(title))
                       .Where(title => !title.Contains(':', StringComparison.Ordinal))
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList()
                   ?? [];
        }

        private static string? ExtractInfoboxTemplateName(string rawWikiText)
        {
            Match match = InfoboxTemplateRegex().Match(rawWikiText);
            return match.Success ? match.Groups["name"].Value.Trim() : null;
        }

        private static string? ExtractInfoboxJson(string rawWikiText)
        {
            Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

            foreach(Match match in InfoboxParameterRegex().Matches(rawWikiText))
            {
                string key = NormalizeText(match.Groups["key"].Value);
                string value = NormalizeText(match.Groups["value"].Value);

                if(string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                values.TryAdd(key, value);
            }

            return values.Count == 0 ? null : JsonSerializer.Serialize(values);
        }

        private static string? ExtractAdditionalAttributes(string rawWikiText, WikiContentType contentType)
        {
            
            if(contentType != WikiContentType.HuntingPlace)
            {
                return null;
            }

            Dictionary<string, string> infoboxValues = new(StringComparer.OrdinalIgnoreCase);

            foreach(Match match in InfoboxParameterRegex().Matches(rawWikiText))
            {
                string key = NormalizeText(match.Groups["key"].Value);
                string value = NormalizeText(match.Groups["value"].Value);

                if(string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                infoboxValues.TryAdd(key, value);
            }

            
            if(!infoboxValues.ContainsKey("lowerlevels"))
            {
                return null;
            }

            
            List<string> keys = infoboxValues.Keys.ToList();
            int lowerLevelsIndex = keys.FindIndex(k => k.Equals("lowerlevels", StringComparison.OrdinalIgnoreCase));

            if(lowerLevelsIndex < 0 || lowerLevelsIndex >= keys.Count - 1)
            {
                return null;
            }

            
            HashSet<string> lowerLevelKeys = new(StringComparer.OrdinalIgnoreCase)
            {
                "areaname",
                "lvlknights",
                "lvlpaladins",
                "lvlmages",
                "skknights",
                "skpaladins",
                "skmages",
                "defknights",
                "defpaladins",
                "defmages",
                "loot",
                "exp",
                "expstar",
                "lootstar"
            };

            
            Dictionary<string, string> lowerLevelData = new(StringComparer.OrdinalIgnoreCase);

            for (int i = lowerLevelsIndex + 1; i < keys.Count; i++)
            {
                string key = keys[i];
                if(lowerLevelKeys.Contains(key) && infoboxValues.TryGetValue(key, out string? value))
                {
                    lowerLevelData[key] = value;
                }
            }

            if(lowerLevelData.Count == 0)
            {
                return null;
            }

            
            List<Dictionary<string, string>> lowerLevels = [lowerLevelData];

            return JsonSerializer.Serialize(new
            {
                LowerLevels = lowerLevels
            });
        }

        private static string NormalizeText(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized = HtmlEntity.DeEntitize(value);
            normalized = WhitespaceRegex().Replace(normalized, " ").Trim();
            return normalized;
        }

        [GeneratedRegex(@"^\s*\{\{\s*(?<name>[^\|\r\n\}]+)", RegexOptions.Multiline)]
        private static partial Regex InfoboxTemplateRegex();

        [GeneratedRegex(@"(?m)^\|\s*(?<key>[^=\r\n]+?)\s*=\s*(?<value>.+)$")]
        private static partial Regex InfoboxParameterRegex();

        [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
        private static partial Regex WhitespaceRegex();

        protected internal readonly record struct FetchedArticleResult(
            string Title,
            string? RawWikiText,
            WikiArticle? Article,
            Exception? Exception);

        private readonly record struct WikiArticleChangeOutcome(string? Title, string? NormalizedTitle);
    }
}