using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.Concurrency;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Scraper
{
    public abstract class BaseScraper(
        ITibiaWikiHttpService tibiaWikiHttpService,
        IItemImageSyncService itemImageSyncService,
        ILogger logger) : WikiScraperBase(tibiaWikiHttpService, logger)
    {
        protected readonly IItemImageSyncService ItemImageSyncService = itemImageSyncService;

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

            HashSet<string> seenItemNames = new(StringComparer.Ordinal);
            int processedCount = 0;

            foreach(string title in titles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    string content = await GetWikiTextAsync(title, cancellationToken);

                    if(string.IsNullOrWhiteSpace(content))
                    {
                        RecordFailure(
                            db,
                            scrapeLog,
                            title,
                            title,
                            "NoContent",
                            "No wikitext content was returned for the requested page.",
                            null);
                        continue;
                    }

                    Item item = BuildItem(title, content, category);
                    ItemChangeOutcome outcome = await UpsertItemAsync(db, scrapeLog, item, cancellationToken);

                    if(!string.IsNullOrWhiteSpace(outcome.Item?.NormalizedName))
                    {
                        seenItemNames.Add(outcome.Item.NormalizedName);
                    }

                    if(outcome.Item is not null)
                    {
                        if(outcome.Item.Id <= 0)
                        {
                            await db.SaveChangesAsync(cancellationToken);
                        }

                        await ItemImageSyncService.QueuePrimaryImageSyncAsync(
                            outcome.Item.Id,
                            title,
                            outcome.RequiresImageSync,
                            cancellationToken);
                    }

                    processedCount++;

                    if(processedCount % 25 == 0)
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    RecordFailure(db, scrapeLog, title, title, ex.GetType().Name, ex.Message, ex);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            await MarkMissingFromSourceAsync(db, scrapeLog, category, seenItemNames, cancellationToken);
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

        protected abstract Item BuildItem(string title, string content, WikiCategory category);

        private async Task<ItemChangeOutcome> UpsertItemAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            Item item,
            CancellationToken cancellationToken)
        {
            using IDisposable itemLock = await AsyncKeyedLockProvider.AcquireAsync(
                "item",
                item.NormalizedName,
                cancellationToken).ConfigureAwait(false);

            Item? existing = await db.Items.FirstOrDefaultAsync(entry => entry.NormalizedName == item.NormalizedName, cancellationToken);

            scrapeLog.PagesProcessed++;
            scrapeLog.ItemsProcessed++;

            if(existing is null)
            {
                item.LastSeenAt = DateTime.UtcNow;
                item.LastUpdated = DateTime.UtcNow;
                item.IsMissingFromSource = false;
                item.MissingSince = null;

                db.Items.Add(item);
                db.ScrapeItemChanges.Add(new ScrapeItemChange
                {
                    ScrapeLogId = scrapeLog.Id,
                    ItemName = item.Name,
                    ChangeType = ScrapeChangeType.Added,
                    CategorySlug = RuntimeCategorySlug,
                    CategoryName = CategoryDefinition.Name,
                    AfterJson = ItemChangeDetector.CreateSnapshotJson(item)
                });

                scrapeLog.ItemsAdded++;
                UpdateChangesSummary(scrapeLog);
                await db.SaveChangesAsync(cancellationToken);

                return new ItemChangeOutcome(item.Name, item, true);
            }

            List<string> changedFields = ItemChangeDetector.GetChangedFields(existing, item).ToList();
            string beforeJson = ItemChangeDetector.CreateSnapshotJson(existing);
            bool wasMissingFromSource = existing.IsMissingFromSource;

            ApplyCurrentValues(existing, item);

            existing.LastSeenAt = DateTime.UtcNow;
            existing.IsMissingFromSource = false;
            existing.MissingSince = null;

            if(wasMissingFromSource)
            {
                changedFields.Add(nameof(Item.IsMissingFromSource));
            }

            if(changedFields.Count == 0)
            {
                scrapeLog.ItemsUnchanged++;
                UpdateChangesSummary(scrapeLog);
                return new ItemChangeOutcome(existing.Name, existing, false);
            }

            existing.LastUpdated = DateTime.UtcNow;

            db.ScrapeItemChanges.Add(new ScrapeItemChange
            {
                ScrapeLogId = scrapeLog.Id,
                ItemId = existing.Id,
                ItemName = existing.Name,
                ChangeType = ScrapeChangeType.Updated,
                CategorySlug = RuntimeCategorySlug,
                CategoryName = CategoryDefinition.Name,
                ChangedFieldsJson = JsonSerializer.Serialize(changedFields),
                BeforeJson = beforeJson,
                AfterJson = ItemChangeDetector.CreateSnapshotJson(existing)
            });

            scrapeLog.ItemsUpdated++;
            UpdateChangesSummary(scrapeLog);
            await db.SaveChangesAsync(cancellationToken);

            return new ItemChangeOutcome(existing.Name, existing, true);
        }

        private async Task MarkMissingFromSourceAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            WikiCategory category,
            IReadOnlySet<string> seenItemNames,
            CancellationToken cancellationToken)
        {
            List<Item> categoryItems = await db.Items
                                               .Where(item => item.CategoryId == category.Id)
                                               .Where(item => !item.IsMissingFromSource)
                                               .ToListAsync(cancellationToken);

            List<Item> missingItems = categoryItems
                                      .Where(item => !seenItemNames.Contains(item.NormalizedName))
                                      .ToList();

            foreach(Item item in missingItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using IDisposable itemLock = await AsyncKeyedLockProvider.AcquireAsync(
                    "item",
                    item.NormalizedName,
                    cancellationToken).ConfigureAwait(false);

                Item? trackedItem = await db.Items.FirstOrDefaultAsync(entry => entry.Id == item.Id, cancellationToken);
                if(trackedItem is null || trackedItem.IsMissingFromSource)
                {
                    continue;
                }

                string beforeJson = ItemChangeDetector.CreateSnapshotJson(trackedItem);

                trackedItem.IsMissingFromSource = true;
                trackedItem.MissingSince = DateTime.UtcNow;
                trackedItem.LastUpdated = DateTime.UtcNow;

                db.ScrapeItemChanges.Add(new ScrapeItemChange
                {
                    ScrapeLogId = scrapeLog.Id,
                    ItemId = trackedItem.Id,
                    ItemName = trackedItem.Name,
                    ChangeType = ScrapeChangeType.MissingFromSource,
                    CategorySlug = RuntimeCategorySlug,
                    CategoryName = CategoryDefinition.Name,
                    BeforeJson = beforeJson,
                    AfterJson = ItemChangeDetector.CreateSnapshotJson(trackedItem)
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
            string itemName,
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
                ItemName = itemName,
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

        private static void ApplyCurrentValues(Item existing, Item incoming)
        {
            existing.CategoryId = incoming.CategoryId;
            existing.Name = incoming.Name;
            existing.NormalizedName = incoming.NormalizedName;
            existing.ActualName = incoming.ActualName;
            existing.NormalizedActualName = incoming.NormalizedActualName;
            existing.Plural = incoming.Plural;
            existing.Article = incoming.Article;
            existing.Implemented = incoming.Implemented;
            existing.ItemId = incoming.ItemId.ToList();
            existing.DroppedBy = incoming.DroppedBy.ToList();
            existing.Sounds = incoming.Sounds.ToList();
            existing.TemplateType = incoming.TemplateType;
            existing.ObjectClass = incoming.ObjectClass;
            existing.PrimaryType = incoming.PrimaryType;
            existing.SecondaryType = incoming.SecondaryType;
            existing.WeaponType = incoming.WeaponType;
            existing.Hands = incoming.Hands;
            existing.Attack = incoming.Attack;
            existing.Defense = incoming.Defense;
            existing.DefenseMod = incoming.DefenseMod;
            existing.Armor = incoming.Armor;
            existing.Range = incoming.Range;
            existing.LevelRequired = incoming.LevelRequired;
            existing.ImbueSlots = incoming.ImbueSlots;
            existing.Vocation = incoming.Vocation;
            existing.DamageType = incoming.DamageType;
            existing.DamageRange = incoming.DamageRange;
            existing.EnergyAttack = incoming.EnergyAttack;
            existing.FireAttack = incoming.FireAttack;
            existing.EarthAttack = incoming.EarthAttack;
            existing.IceAttack = incoming.IceAttack;
            existing.DeathAttack = incoming.DeathAttack;
            existing.HolyAttack = incoming.HolyAttack;
            existing.Stackable = incoming.Stackable;
            existing.Usable = incoming.Usable;
            existing.Marketable = incoming.Marketable;
            existing.Walkable = incoming.Walkable;
            existing.NpcPrice = incoming.NpcPrice;
            existing.NpcValue = incoming.NpcValue;
            existing.Value = incoming.Value;
            existing.Weight = incoming.Weight;
            existing.Attrib = incoming.Attrib;
            existing.UpgradeClass = incoming.UpgradeClass;
            existing.WikiUrl = incoming.WikiUrl;
            existing.AdditionalAttributesJson = incoming.AdditionalAttributesJson;
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

        private readonly record struct ItemChangeOutcome(string? ItemName, Item? Item, bool RequiresImageSync);
    }
}