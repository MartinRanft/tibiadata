using System.Net;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.Concurrency;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Scraper.Implementations
{
    public partial class CatalogBackedCreatureScraper(
        string categorySlug,
        ITibiaWikiHttpService tibiaWikiHttpService,
        ICreatureImageSyncService creatureImageSyncService,
        ILogger logger) : WikiScraperBase(tibiaWikiHttpService, logger)
    {
        private readonly ICreatureImageSyncService _creatureImageSyncService = creatureImageSyncService;

        protected override string CategorySlug => categorySlug;

        protected override WikiContentType ContentType => WikiContentType.Creature;

        protected override string ScraperName => $"{CategoryDefinition.Name.Replace(" ", string.Empty)}Scraper";

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

            int skippedNonCreaturePages = 0;

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
                            "NoContent",
                            "No wikitext content was returned for the requested page.",
                            null);
                        continue;
                    }

                    if(!LooksLikeCreaturePage(content))
                    {
                        skippedNonCreaturePages++;
                        continue;
                    }

                    Creature creature = BuildCreature(title, content);
                    CreatureChangeOutcome outcome = await UpsertCreatureAsync(db, scrapeLog, creature, cancellationToken);

                    if(outcome.Creature is not null)
                    {
                        await _creatureImageSyncService.QueuePrimaryImageSyncAsync(
                            outcome.Creature.Id,
                            title,
                            outcome.RequiresImageSync,
                            cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    RecordFailure(db, scrapeLog, title, ex.GetType().Name, ex.Message, ex);
                }
            }

            scrapeLog.MetadataJson = JsonSerializer.Serialize(new
            {
                NonCreaturePagesSkipped = skippedNonCreaturePages
            });

            await db.SaveChangesAsync(cancellationToken);

            Logger.LogInformation(
                "{ScraperName} finished. Added={Added}, Updated={Updated}, Unchanged={Unchanged}, Failed={Failed}, NonCreaturePagesSkipped={Skipped}",
                RuntimeScraperName,
                scrapeLog.ItemsAdded,
                scrapeLog.ItemsUpdated,
                scrapeLog.ItemsUnchanged,
                scrapeLog.ItemsFailed,
                skippedNonCreaturePages);
        }

        protected internal virtual Creature BuildCreature(string title, string content)
        {
            string resolvedName = Extract(content, "name", "actualname");
            string creatureName = string.IsNullOrWhiteSpace(resolvedName) ? title : resolvedName;

            return new Creature
            {
                Name = creatureName,
                NormalizedName = EntityNameNormalizer.Normalize(creatureName),
                Hitpoints = ParseInt32(Extract(content, "hp", "hitpoints")),
                Experience = ParseInt64(Extract(content, "exp", "experience")),
                InfoboxJson = BuildInfoboxJson(content),
                BestiaryJson = BuildBestiaryJson(content),
                LootStatisticsJson = BuildLootStatisticsJson(content),
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task<CreatureChangeOutcome> UpsertCreatureAsync(
            TibiaDbContext db,
            ScrapeLog scrapeLog,
            Creature creature,
            CancellationToken cancellationToken)
        {
            using IDisposable creatureLock = await AsyncKeyedLockProvider.AcquireAsync(
                "creature",
                creature.NormalizedName,
                cancellationToken).ConfigureAwait(false);

            Creature? existing = await db.Creatures
                                         .FirstOrDefaultAsync(entry => entry.NormalizedName == creature.NormalizedName, cancellationToken)
                                         .ConfigureAwait(false);

            scrapeLog.PagesProcessed++;
            scrapeLog.ItemsProcessed++;

            if(existing is null)
            {
                db.Creatures.Add(creature);
                db.ScrapeItemChanges.Add(new ScrapeItemChange
                {
                    ScrapeLogId = scrapeLog.Id,
                    ItemName = creature.Name,
                    ChangeType = ScrapeChangeType.Added,
                    CategorySlug = RuntimeCategorySlug,
                    CategoryName = CategoryDefinition.Name,
                    AfterJson = CreateCreatureSnapshotJson(creature)
                });

                scrapeLog.ItemsAdded++;
                UpdateChangesSummary(scrapeLog);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return new CreatureChangeOutcome(creature, true);
            }

            List<string> changedFields = GetChangedFields(existing, creature);

            if(changedFields.Count == 0)
            {
                scrapeLog.ItemsUnchanged++;
                UpdateChangesSummary(scrapeLog);
                return new CreatureChangeOutcome(existing, false);
            }

            string beforeJson = CreateCreatureSnapshotJson(existing);

            existing.Name = creature.Name;
            existing.NormalizedName = creature.NormalizedName;
            existing.Hitpoints = creature.Hitpoints;
            existing.Experience = creature.Experience;
            existing.InfoboxJson = creature.InfoboxJson;
            existing.BestiaryJson = creature.BestiaryJson;
            existing.LootStatisticsJson = creature.LootStatisticsJson;
            existing.LastUpdated = DateTime.UtcNow;

            db.ScrapeItemChanges.Add(new ScrapeItemChange
            {
                ScrapeLogId = scrapeLog.Id,
                ItemName = existing.Name,
                ChangeType = ScrapeChangeType.Updated,
                CategorySlug = RuntimeCategorySlug,
                CategoryName = CategoryDefinition.Name,
                ChangedFieldsJson = JsonSerializer.Serialize(changedFields),
                BeforeJson = beforeJson,
                AfterJson = CreateCreatureSnapshotJson(existing)
            });

            scrapeLog.ItemsUpdated++;
            UpdateChangesSummary(scrapeLog);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new CreatureChangeOutcome(existing, true);
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

        private static string? BuildLootStatisticsJson(string content)
        {
            List<CreatureLootEntry> lootEntries = LootItemTemplateRegex().Matches(content)
                                                                         .Select(match => ParseLootEntry(match.Groups["content"].Value))
                                                                         .Where(entry => entry is not null)
                                                                         .Cast<CreatureLootEntry>()
                                                                         .ToList();

            return lootEntries.Count == 0
            ? null
            : JsonSerializer.Serialize(lootEntries);
        }

        private string? BuildInfoboxJson(string content)
        {
            Dictionary<string, string> infobox = ParseCreatureInfobox(content);
            return infobox.Count == 0 ? null : JsonSerializer.Serialize(infobox);
        }

        private string? BuildBestiaryJson(string content)
        {
            string bestiaryClass = ExtractInlineValue(content, "bestiaryclass");
            string bestiaryLevel = ExtractInlineValue(content, "bestiarylevel");
            string bosstiaryClass = ExtractInlineValue(content, "bosstiaryclass", "bosstiarycategory");

            string? classSlug = null;
            string? difficultySlug = null;
            string? normalizedBosstiaryClass = string.IsNullOrWhiteSpace(bosstiaryClass)
                ? null
                : NormalizeBestiaryValue(bosstiaryClass);

            if(!string.IsNullOrWhiteSpace(bestiaryClass) && !string.IsNullOrWhiteSpace(bestiaryLevel))
            {
                classSlug = NormalizeBestiaryValue(bestiaryClass);
                difficultySlug = NormalizeBestiaryValue(bestiaryLevel);
            }

            bool hasBestiaryData = !string.IsNullOrWhiteSpace(classSlug) && !string.IsNullOrWhiteSpace(difficultySlug);
            bool hasBosstiaryData = !string.IsNullOrWhiteSpace(normalizedBosstiaryClass);

            if(!hasBestiaryData && !hasBosstiaryData)
            {
                return null;
            }

            Dictionary<string, object?> payload = new();

            if(hasBestiaryData)
            {
                payload["classSlug"] = classSlug;
                payload["categorySlug"] = classSlug;
                payload["difficultySlug"] = difficultySlug;
                payload["occurrence"] = ExtractInlineValue(content, "occurrence");
                payload["bestiaryName"] = ExtractInlineValue(content, "bestiaryname");
            }

            if(hasBosstiaryData)
            {
                payload["bosstiaryCategorySlug"] = normalizedBosstiaryClass;
                payload["bosstiaryCategory"] = bosstiaryClass.Trim();
            }

            return JsonSerializer.Serialize(payload);
        }

        private string ExtractInlineValue(string content, params string[] aliases)
        {
            foreach(string key in aliases)
            {
                string pattern = @"\|\s*" + Regex.Escape(key) + @"[^\S\r\n]*=[^\S\r\n]*(.*?)(?=(\r?\n\s*\||\}\}))";
                Match match = Regex.Match(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if(match.Success)
                {
                    return CleanValue(match.Groups[1].Value);
                }
            }

            return string.Empty;
        }

        private Dictionary<string, string> ParseCreatureInfobox(string content)
        {
            string? infoboxTemplate = ExtractCreatureInfoboxTemplate(content);

            if(string.IsNullOrWhiteSpace(infoboxTemplate))
            {
                return [];
            }

            Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
            using StringReader reader = new(infoboxTemplate);

            int templateDepth = 0;
            string? currentKey = null;
            StringBuilder? currentValue = null;

            while(reader.ReadLine() is { } line)
            {
                string trimmedLine = line.TrimStart();
                bool isInfoboxClosingLine = templateDepth == 1 && trimmedLine.StartsWith("}}", StringComparison.Ordinal);
                bool isTopLevelParameter = templateDepth == 1 && !isInfoboxClosingLine && IsTopLevelParameterLine(trimmedLine);

                if(isTopLevelParameter)
                {
                    FlushCurrentInfoboxValue(values, currentKey, currentValue);

                    int equalsIndex = trimmedLine.IndexOf('=');
                    currentKey = NormalizeInfoboxKey(trimmedLine[1..equalsIndex]);
                    currentValue = new StringBuilder();

                    string firstValueLine = trimmedLine[(equalsIndex + 1)..];
                    AppendInfoboxValueLine(currentValue, firstValueLine);
                }
                else if(currentKey is not null && !isInfoboxClosingLine && templateDepth >= 1)
                {
                    AppendInfoboxValueLine(currentValue!, line);
                }

                templateDepth += CountTemplateOpenings(line);
                templateDepth -= CountTemplateClosings(line);
            }

            FlushCurrentInfoboxValue(values, currentKey, currentValue);
            return values;
        }

        private static CreatureLootEntry? ParseLootEntry(string rawContent)
        {
            List<string> parts = rawContent.Split('|')
                                           .Select(part => part.Trim())
                                           .Where(part => !string.IsNullOrWhiteSpace(part))
                                           .ToList();

            if(parts.Count == 0)
            {
                return null;
            }

            string? chance = null;
            string itemName;
            string? rarity = null;

            if(parts.Count == 1)
            {
                itemName = NormalizeLootValue(parts[0]);
            }
            else
            {
                chance = LooksLikeChance(parts[0]) ? NormalizeLootValue(parts[0]) : null;
                itemName = NormalizeLootValue(chance is null ? parts[0] : parts[1]);

                int rarityIndex = chance is null ? 1 : 2;
                if(parts.Count > rarityIndex)
                {
                    rarity = NormalizeLootValue(parts[rarityIndex]);
                }
            }

            return string.IsNullOrWhiteSpace(itemName)
            ? null
            : new CreatureLootEntry(itemName, chance, rarity, rawContent.Trim());
        }

        private static string NormalizeLootValue(string value)
        {
            string cleaned = Regex.Replace(value, @"\[\[(?:[^\]|]+\|)?([^\]]+)\]\]", "$1");
            cleaned = Regex.Replace(cleaned, "<.*?>", " ");
            return WebUtility.HtmlDecode(cleaned.Replace("&nbsp;", " ")).Trim();
        }

        private static bool LooksLikeCreaturePage(string content)
        {
            return InfoboxCreatureRegex().IsMatch(content);
        }

        private static bool LooksLikeChance(string value)
        {
            return ChanceValueRegex().IsMatch(value.Trim());
        }

        private static int ParseInt32(string value)
        {
            Match match = NumberRegex().Match(value);
            return match.Success && int.TryParse(match.Value.Replace(",", string.Empty), out int result) ? result : 0;
        }

        private static long ParseInt64(string value)
        {
            Match match = NumberRegex().Match(value);
            return match.Success && long.TryParse(match.Value.Replace(",", string.Empty), out long result) ? result : 0;
        }

        private static List<string> GetChangedFields(Creature existing, Creature incoming)
        {
            List<string> changedFields = [];

            if(!string.Equals(existing.Name, incoming.Name, StringComparison.Ordinal))
            {
                changedFields.Add(nameof(Creature.Name));
            }

            if(existing.Hitpoints != incoming.Hitpoints)
            {
                changedFields.Add(nameof(Creature.Hitpoints));
            }

            if(existing.Experience != incoming.Experience)
            {
                changedFields.Add(nameof(Creature.Experience));
            }

            if(!string.Equals(existing.InfoboxJson, incoming.InfoboxJson, StringComparison.Ordinal))
            {
                changedFields.Add(nameof(Creature.InfoboxJson));
            }

            if(!string.Equals(existing.BestiaryJson, incoming.BestiaryJson, StringComparison.Ordinal))
            {
                changedFields.Add(nameof(Creature.BestiaryJson));
            }

            if(!string.Equals(existing.LootStatisticsJson, incoming.LootStatisticsJson, StringComparison.Ordinal))
            {
                changedFields.Add(nameof(Creature.LootStatisticsJson));
            }

            return changedFields;
        }

        private static string CreateCreatureSnapshotJson(Creature creature)
        {
            return JsonSerializer.Serialize(new
            {
                creature.Name,
                creature.Hitpoints,
                creature.Experience,
                creature.InfoboxJson,
                creature.BestiaryJson,
                creature.LootStatisticsJson
            });
        }

        private static string NormalizeBestiaryValue(string value)
        {
            return value.Trim()
                        .ToLowerInvariant()
                        .Replace('_', '-')
                        .Replace(' ', '-');
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

        private static string? ExtractCreatureInfoboxTemplate(string content)
        {
            Match match = InfoboxCreatureRegex().Match(content);

            if(!match.Success)
            {
                return null;
            }

            int startIndex = match.Index;
            int depth = 0;

            for(int i = startIndex; i < content.Length - 1; i++)
            {
                if(content[i] == '{' && content[i + 1] == '{')
                {
                    depth++;
                    i++;
                    continue;
                }

                if(content[i] == '}' && content[i + 1] == '}')
                {
                    depth--;
                    i++;

                    if(depth == 0)
                    {
                        return content[startIndex..(i + 1)];
                    }
                }
            }

            return null;
        }

        private static bool IsTopLevelParameterLine(string line)
        {
            if(!line.StartsWith('|'))
            {
                return false;
            }

            int equalsIndex = line.IndexOf('=');
            return equalsIndex > 1;
        }

        private string NormalizeInfoboxKey(string rawKey)
        {
            return CleanValue(rawKey).Replace(" ", string.Empty).ToLowerInvariant();
        }

        private void AppendInfoboxValueLine(StringBuilder builder, string line)
        {
            if(builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(line);
        }

        private void FlushCurrentInfoboxValue(
            IDictionary<string, string> values,
            string? key,
            StringBuilder? valueBuilder)
        {
            if(string.IsNullOrWhiteSpace(key) || valueBuilder is null)
            {
                return;
            }

            string value = CleanValue(valueBuilder.ToString());

            if(string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            values[key] = value;
        }

        private static int CountTemplateOpenings(string line)
        {
            return CountOccurrences(line, "{{");
        }

        private static int CountTemplateClosings(string line)
        {
            return CountOccurrences(line, "}}");
        }

        private static int CountOccurrences(string value, string pattern)
        {
            int count = 0;
            int index = 0;

            while((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }

        [GeneratedRegex(@"\{\{\s*Infobox Creature\b", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
        private static partial Regex InfoboxCreatureRegex();

        [GeneratedRegex(@"\{\{Loot Item\|(?<content>.*?)\}\}", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
        private static partial Regex LootItemTemplateRegex();

        [GeneratedRegex(@"^[\d,\-–%? ]+$", RegexOptions.Compiled, "en-US")]
        private static partial Regex ChanceValueRegex();

        [GeneratedRegex(@"[\d,]+", RegexOptions.Compiled, "en-US")]
        private static partial Regex NumberRegex();

        private sealed record CreatureLootEntry(
            string ItemName,
            string? Chance,
            string? Rarity,
            string Raw);

        private sealed record CreatureChangeOutcome(
            Creature? Creature,
            bool RequiresImageSync);
    }
}
