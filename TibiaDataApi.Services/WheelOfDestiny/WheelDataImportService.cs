using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Entities.WheelOfDestiny;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;
using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.WheelOfDestiny
{
    public sealed partial class WheelDataImportService(
        ILogger<WheelDataImportService> logger,
        IWheelPlannerLayoutSource wheelPlannerLayoutSource,
        IGemModDataImportService gemModDataImportService) : IWheelDataImportService
    {
        private static readonly WheelVocation[] AllVocations =
        [
            WheelVocation.EliteKnight,
            WheelVocation.RoyalPaladin,
            WheelVocation.ElderDruid,
            WheelVocation.MasterSorcerer,
            WheelVocation.ExaltedMonk
        ];

        private static readonly IReadOnlyDictionary<string, WheelVocation[]> ConvictionSectionVocationalMap =
            new Dictionary<string, WheelVocation[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Elite Knight"] = [WheelVocation.EliteKnight],
                ["Royal Paladin"] = [WheelVocation.RoyalPaladin],
                ["Elder Druid"] = [WheelVocation.ElderDruid],
                ["Master Sorcerer"] = [WheelVocation.MasterSorcerer],
                ["Mages"] = [WheelVocation.ElderDruid, WheelVocation.MasterSorcerer],
                ["Exalted Monk"] = [WheelVocation.ExaltedMonk]
            };

        private static readonly IReadOnlyDictionary<string, WheelVocation[]> RevelationSectionVocationalMap =
            new Dictionary<string, WheelVocation[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["All vocations"] = AllVocations,
                ["Elite Knights"] = [WheelVocation.EliteKnight],
                ["Royal Paladins"] = [WheelVocation.RoyalPaladin],
                ["Elder Druid"] = [WheelVocation.ElderDruid],
                ["Master Sorcerer"] = [WheelVocation.MasterSorcerer],
                ["Exalted Monk"] = [WheelVocation.ExaltedMonk]
            };

        private static readonly IReadOnlyDictionary<WheelVocation, string> AvatarSourceTitleByVocation =
            new Dictionary<WheelVocation, string>
            {
                [WheelVocation.EliteKnight] = "Avatar of Steel",
                [WheelVocation.RoyalPaladin] = "Avatar of Light",
                [WheelVocation.ElderDruid] = "Avatar of Nature",
                [WheelVocation.MasterSorcerer] = "Avatar of Storm",
                [WheelVocation.ExaltedMonk] = "Avatar of Balance"
            };

        private static readonly IReadOnlyDictionary<string, string> DedicatedRevelationSourceTitleByPerkName =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Gift of Life"] = "Gift of Life",
                ["Executioner's Throw"] = "Executioner's Throw",
                ["Divine Grenade"] = "Divine Grenade",
                ["Divine Empowerment"] = "Divine Empowerment",
                ["Spiritual Outburst"] = "Spiritual Outburst"
            };

        private static readonly IReadOnlyDictionary<string, string> PlannerPerkNameAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Mitigation Multiplier"] = "Mitigation",
                ["Weapon Skill Boost"] = "Sword/Axe/Club Fighting",
                ["Distance Skill Boost"] = "Distance Fighting",
                ["Magic Skill Boost"] = "Magic Level",
                ["Fist Fighting Skill Boost"] = "Fist Fighting"
            };

        private static readonly IReadOnlyDictionary<string, string[]> PlannerPerkNameFallbacks =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Mitigation"] = ["Mitigation Multiplier"],
                ["Sword/Axe/Club Fighting"] = ["Weapon, Distance, Magic Skill Boosts"],
                ["Distance Fighting"] = ["Weapon, Distance, Magic Skill Boosts"],
                ["Magic Level"] = ["Weapon, Distance, Magic Skill Boosts"],
                ["Fist Fighting"] = ["Weapon, Distance, Magic Skill Boosts"]
            };

        private const short DedicationOccurrenceRequiredPoints = 1;
        private const short ConvictionSliceMaxPoints = 25;
        private const short RevelationMinimumDomainPoints = 250;

        public async Task<WheelDataImportResult> ImportAsync(
            TibiaDbContext db,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IExecutionStrategy executionStrategy = db.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using IDbContextTransaction? transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(cancellationToken)
                : null;

                Dictionary<string, WikiArticle> sourceArticles = await LoadSourceArticlesAsync(db, cancellationToken);
                HashSet<string> usedSourceTitles = new(StringComparer.OrdinalIgnoreCase);

                List<WheelPerkDraft> drafts = [];
                drafts.AddRange(ParseDedicationPerks(sourceArticles, usedSourceTitles));
                drafts.AddRange(ParseConvictionPerks(sourceArticles, usedSourceTitles));
                drafts.AddRange(ParseRevelationPerks(sourceArticles, usedSourceTitles));

                List<WheelPerkDraft> normalizedDrafts = drafts
                                                        .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                                                        .ToList();

                List<WheelPerk> existing = await db.WheelPerks
                                                   .Include(entry => entry.Occurrences)
                                                   .Include(entry => entry.Stages)
                                                   .ToListAsync(cancellationToken);

                Dictionary<string, WheelPerk> existingByKey = existing.ToDictionary(entry => entry.Key, StringComparer.OrdinalIgnoreCase);
                HashSet<string> incomingKeys = normalizedDrafts
                                               .Select(entry => entry.Key)
                                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

                DateTime now = DateTime.UtcNow;
                int added = 0;
                int updated = 0;
                int unchanged = 0;

                foreach(WheelPerkDraft draft in normalizedDrafts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if(!existingByKey.TryGetValue(draft.Key, out WheelPerk? tracked))
                    {
                        db.WheelPerks.Add(CreateEntity(draft, now));
                        added++;
                        continue;
                    }

                    string existingSnapshot = CreateSnapshot(tracked);
                    string incomingSnapshot = CreateSnapshot(draft);

                    if(string.Equals(existingSnapshot, incomingSnapshot, StringComparison.Ordinal))
                    {
                        unchanged++;
                        continue;
                    }

                    ApplyDraft(tracked, draft, now);
                    updated++;
                }

                List<WheelPerk> removedPerks = existing
                                               .Where(entry => !incomingKeys.Contains(entry.Key))
                                               .ToList();

                if(removedPerks.Count > 0)
                {
                    db.WheelPerks.RemoveRange(removedPerks);
                }

                await db.SaveChangesAsync(cancellationToken);
                await SynchronizePlannerLayoutAsync(db, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                
                GemImportResult gemImportResult = await gemModDataImportService.ImportGemsAsync(db, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                if(transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                logger.LogInformation(
                    "Wheel data import rebuilt {PerkCount} perks from {SourceArticleCount} source articles. Gems: {GemsProcessed} processed, {GemsAdded} added, {GemsUpdated} updated.",
                    normalizedDrafts.Count,
                    usedSourceTitles.Count,
                    gemImportResult.GemsProcessed,
                    gemImportResult.Added,
                    gemImportResult.Updated);

                return new WheelDataImportResult(
                    usedSourceTitles.Count,
                    normalizedDrafts.Count,
                    added,
                    updated,
                    unchanged,
                    removedPerks.Count,
                    gemImportResult);
            });
        }

        private static WheelPerk CreateEntity(WheelPerkDraft draft, DateTime timestampUtc)
        {
            return new WheelPerk
            {
                Key = draft.Key,
                Slug = draft.Slug,
                Vocation = draft.Vocation,
                Type = draft.Type,
                Name = draft.Name,
                Summary = draft.Summary,
                Description = draft.Description,
                MainSourceTitle = draft.MainSourceTitle,
                MainSourceUrl = draft.MainSourceUrl,
                IsGenericAcrossVocations = draft.IsGenericAcrossVocations,
                IsActive = true,
                MetadataJson = draft.MetadataJson,
                LastUpdated = timestampUtc,
                Occurrences = draft.Occurrences
                                   .Select(entry => new WheelPerkOccurrence
                                   {
                                       Domain = entry.Domain,
                                       OccurrenceIndex = entry.OccurrenceIndex,
                                       RequiredPoints = entry.RequiredPoints,
                                       IsStackable = entry.IsStackable,
                                       Notes = entry.Notes
                                   })
                                   .ToList(),
                Stages = draft.Stages
                              .Select(entry => new WheelPerkStage
                              {
                                  Stage = entry.Stage,
                                  UnlockKind = entry.UnlockKind,
                                  UnlockValue = entry.UnlockValue,
                                  EffectSummary = entry.EffectSummary,
                                  EffectDetailsJson = entry.EffectDetailsJson,
                                  SortOrder = entry.SortOrder
                              })
                              .ToList()
            };
        }

        private static void ApplyDraft(WheelPerk tracked, WheelPerkDraft draft, DateTime timestampUtc)
        {
            tracked.Slug = draft.Slug;
            tracked.Vocation = draft.Vocation;
            tracked.Type = draft.Type;
            tracked.Name = draft.Name;
            tracked.Summary = draft.Summary;
            tracked.Description = draft.Description;
            tracked.MainSourceTitle = draft.MainSourceTitle;
            tracked.MainSourceUrl = draft.MainSourceUrl;
            tracked.IsGenericAcrossVocations = draft.IsGenericAcrossVocations;
            tracked.IsActive = true;
            tracked.MetadataJson = draft.MetadataJson;
            tracked.LastUpdated = timestampUtc;

            SynchronizeOccurrences(tracked, draft.Occurrences);
            SynchronizeStages(tracked, draft.Stages);
        }

        private static void SynchronizeOccurrences(
            WheelPerk tracked,
            IReadOnlyList<WheelPerkOccurrenceDraft> occurrences)
        {
            Dictionary<short, WheelPerkOccurrence> existingByIndex = tracked.Occurrences
                                                                            .ToDictionary(
                                                                                entry => entry.OccurrenceIndex,
                                                                                entry => entry);

            HashSet<short> incomingIndexes = occurrences
                                             .Select(entry => entry.OccurrenceIndex)
                                             .ToHashSet();

            foreach(WheelPerkOccurrence trackedOccurrence in tracked.Occurrences
                                                                   .Where(entry => !incomingIndexes.Contains(entry.OccurrenceIndex))
                                                                   .ToList())
            {
                tracked.Occurrences.Remove(trackedOccurrence);
            }

            foreach(WheelPerkOccurrenceDraft occurrence in occurrences.OrderBy(entry => entry.OccurrenceIndex))
            {
                if(!existingByIndex.TryGetValue(occurrence.OccurrenceIndex, out WheelPerkOccurrence? trackedOccurrence))
                {
                    tracked.Occurrences.Add(new WheelPerkOccurrence
                    {
                        OccurrenceIndex = occurrence.OccurrenceIndex,
                        Domain = occurrence.Domain,
                        RequiredPoints = occurrence.RequiredPoints,
                        IsStackable = occurrence.IsStackable,
                        Notes = occurrence.Notes
                    });
                    continue;
                }

                trackedOccurrence.Domain = occurrence.Domain;
                trackedOccurrence.RequiredPoints = occurrence.RequiredPoints;
                trackedOccurrence.IsStackable = occurrence.IsStackable;
                trackedOccurrence.Notes = occurrence.Notes;
            }
        }

        private static void SynchronizeStages(
            WheelPerk tracked,
            IReadOnlyList<WheelPerkStageDraft> stages)
        {
            Dictionary<byte, WheelPerkStage> existingByStage = tracked.Stages
                                                                      .ToDictionary(entry => entry.Stage, entry => entry);

            HashSet<byte> incomingStages = stages
                                           .Select(entry => entry.Stage)
                                           .ToHashSet();

            foreach(WheelPerkStage trackedStage in tracked.Stages
                                                          .Where(entry => !incomingStages.Contains(entry.Stage))
                                                          .ToList())
            {
                tracked.Stages.Remove(trackedStage);
            }

            foreach(WheelPerkStageDraft stage in stages.OrderBy(entry => entry.SortOrder))
            {
                if(!existingByStage.TryGetValue(stage.Stage, out WheelPerkStage? trackedStage))
                {
                    tracked.Stages.Add(new WheelPerkStage
                    {
                        Stage = stage.Stage,
                        UnlockKind = stage.UnlockKind,
                        UnlockValue = stage.UnlockValue,
                        EffectSummary = stage.EffectSummary,
                        EffectDetailsJson = stage.EffectDetailsJson,
                        SortOrder = stage.SortOrder
                    });
                    continue;
                }

                trackedStage.UnlockKind = stage.UnlockKind;
                trackedStage.UnlockValue = stage.UnlockValue;
                trackedStage.EffectSummary = stage.EffectSummary;
                trackedStage.EffectDetailsJson = stage.EffectDetailsJson;
                trackedStage.SortOrder = stage.SortOrder;
            }
        }

        private static string CreateSnapshot(WheelPerk entity)
        {
            return JsonSerializer.Serialize(new
            {
                entity.Key,
                entity.Slug,
                entity.Vocation,
                entity.Type,
                entity.Name,
                entity.Summary,
                entity.Description,
                entity.MainSourceTitle,
                entity.MainSourceUrl,
                entity.IsGenericAcrossVocations,
                entity.IsActive,
                entity.MetadataJson,
                Occurrences = entity.Occurrences
                                    .OrderBy(entry => entry.OccurrenceIndex)
                                    .Select(entry => new
                                    {
                                        entry.Domain,
                                        entry.OccurrenceIndex,
                                        entry.RequiredPoints,
                                        entry.IsStackable,
                                        entry.Notes
                                    }),
                Stages = entity.Stages
                               .OrderBy(entry => entry.SortOrder)
                               .Select(entry => new
                               {
                                   entry.Stage,
                                   entry.UnlockKind,
                                   entry.UnlockValue,
                                   entry.EffectSummary,
                                   entry.EffectDetailsJson,
                                   entry.SortOrder
                               })
            });
        }

        private static string CreateSnapshot(WheelPerkDraft draft)
        {
            return JsonSerializer.Serialize(new
            {
                draft.Key,
                draft.Slug,
                draft.Vocation,
                draft.Type,
                draft.Name,
                draft.Summary,
                draft.Description,
                draft.MainSourceTitle,
                draft.MainSourceUrl,
                draft.IsGenericAcrossVocations,
                IsActive = true,
                draft.MetadataJson,
                Occurrences = draft.Occurrences
                                   .OrderBy(entry => entry.OccurrenceIndex)
                                   .Select(entry => new
                                   {
                                       entry.Domain,
                                       entry.OccurrenceIndex,
                                       entry.RequiredPoints,
                                       entry.IsStackable,
                                       entry.Notes
                                   }),
                Stages = draft.Stages
                              .OrderBy(entry => entry.SortOrder)
                              .Select(entry => new
                              {
                                  entry.Stage,
                                  entry.UnlockKind,
                                  entry.UnlockValue,
                                  entry.EffectSummary,
                                  entry.EffectDetailsJson,
                                  entry.SortOrder
                              })
            });
        }

        private async Task<Dictionary<string, WikiArticle>> LoadSourceArticlesAsync(
            TibiaDbContext db,
            CancellationToken cancellationToken)
        {
            List<WikiArticle> articles = await db.WikiArticles
                                                 .AsNoTracking()
                                                 .Where(entry => !entry.IsMissingFromSource)
                                                 .Where(entry =>
                                                     entry.Title == "Wheel of Destiny/Dedication Perks" ||
                                                     entry.Title == "Wheel of Destiny/Conviction Perks" ||
                                                     entry.Title == "Wheel of Destiny/Revelation Perks" ||
                                                     entry.Title == "Gift of Life" ||
                                                     entry.Title == "Avatar of Steel" ||
                                                     entry.Title == "Avatar of Light" ||
                                                     entry.Title == "Avatar of Nature" ||
                                                     entry.Title == "Avatar of Storm" ||
                                                     entry.Title == "Avatar of Balance" ||
                                                     entry.Title == "Executioner's Throw" ||
                                                     entry.Title == "Divine Grenade" ||
                                                     entry.Title == "Divine Empowerment" ||
                                                     entry.Title == "Ice Burst" ||
                                                     entry.Title == "Terra Burst" ||
                                                     entry.Title == "Spiritual Outburst")
                                                 .OrderByDescending(entry => entry.LastUpdated)
                                                 .ToListAsync(cancellationToken);

            Dictionary<string, WikiArticle> byTitle = new(StringComparer.OrdinalIgnoreCase);

            foreach(WikiArticle article in articles)
            {
                byTitle.TryAdd(article.Title, article);
            }

            string[] requiredTitles =
            [
                "Wheel of Destiny/Dedication Perks",
                "Wheel of Destiny/Conviction Perks",
                "Wheel of Destiny/Revelation Perks"
            ];

            List<string> missingRequiredTitles = requiredTitles
                                                 .Where(title => !byTitle.ContainsKey(title))
                                                 .ToList();

            if(missingRequiredTitles.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Wheel data import requires these wiki_articles first: {string.Join(", ", missingRequiredTitles)}.");
            }

            return byTitle;
        }

        private IReadOnlyList<WheelPerkDraft> ParseDedicationPerks(
            IReadOnlyDictionary<string, WikiArticle> sourceArticles,
            ISet<string> usedSourceTitles)
        {
            WikiArticle article = sourceArticles["Wheel of Destiny/Dedication Perks"];
            usedSourceTitles.Add(article.Title);

            List<WheelPerkDraft> drafts = [];

            foreach((string name, string description) in ParseBulletPerks(article.RawWikiText ?? string.Empty))
            {
                foreach(WheelVocation vocation in AllVocations)
                {
                    drafts.Add(CreatePerkDraft(
                        vocation,
                        WheelPerkType.Dedication,
                        name,
                        description,
                        description,
                        article.Title,
                        article.WikiUrl,
                        true,
                        new
                        {
                            SourcePageTitle = article.Title,
                            SourceKind = "wiki-page-bullet-list"
                        },
                            [
                                new WheelPerkOccurrenceDraft(
                                    null,
                                    1,
                                    DedicationOccurrenceRequiredPoints,
                                    true,
                                    "Dedication perks begin scaling with the first invested point in the slice.")
                        ],
                        []));
                }
            }

            return drafts;
        }

        private IReadOnlyList<WheelPerkDraft> ParseConvictionPerks(
            IReadOnlyDictionary<string, WikiArticle> sourceArticles,
            ISet<string> usedSourceTitles)
        {
            WikiArticle article = sourceArticles["Wheel of Destiny/Conviction Perks"];
            usedSourceTitles.Add(article.Title);

            WikiSectionNode root = ParseSections(article.RawWikiText ?? string.Empty);
            List<WheelPerkDraft> drafts = [];

            WikiSectionNode? genericSection = root.Children
                                                 .FirstOrDefault(entry =>
                                                 string.Equals(entry.Title, "Generic Conviction Perks", StringComparison.OrdinalIgnoreCase));

            if(genericSection is not null)
            {
                foreach((string name, string description) in ParseBulletPerks(genericSection.Content))
                {
                    foreach(WheelVocation vocation in AllVocations)
                    {
                        drafts.Add(CreatePerkDraft(
                            vocation,
                            WheelPerkType.Conviction,
                            name,
                            description,
                            description,
                            article.Title,
                            article.WikiUrl,
                            true,
                            new
                            {
                                SourcePageTitle = article.Title,
                                SourceSectionTitle = genericSection.Title,
                                SourceKind = "wiki-page-bullet-list"
                            },
                            [
                                new WheelPerkOccurrenceDraft(
                                    null,
                                    1,
                                    GetConvictionOccurrenceRequiredPoints(1),
                                    true,
                                    "Conviction perks can appear on multiple slices and usually stack.")
                            ],
                            []));
                    }
                }
            }

            foreach(WikiSectionNode section in root.Children)
            {
                if(!ConvictionSectionVocationalMap.TryGetValue(section.Title, out WheelVocation[]? vocations))
                {
                    continue;
                }

                foreach(WikiSectionNode perkSection in section.Children)
                {
                    if(string.Equals(perkSection.Title, "Augmentations", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach((string name, IReadOnlyList<WheelPerkStageDraft> stages) in ParseAugmentationTable(perkSection.Content))
                        {
                            string augmentationDescription = $"Conviction augmentation for {name}.";

                            foreach(WheelVocation vocation in vocations)
                            {
                                drafts.Add(CreatePerkDraft(
                                    vocation,
                                    WheelPerkType.Conviction,
                                    name,
                                    augmentationDescription,
                                    augmentationDescription,
                                    article.Title,
                                    article.WikiUrl,
                                    vocations.Length > 1,
                                    new
                                    {
                                        SourcePageTitle = article.Title,
                                        SourceSectionTitle = section.Title,
                                        SourceSubSectionTitle = perkSection.Title,
                                        SourceKind = "wiki-page-table",
                                        IsAugmentation = true
                                    },
                                    [
                                        new WheelPerkOccurrenceDraft(
                                            null,
                                            1,
                                            GetConvictionOccurrenceRequiredPoints(1),
                                            false,
                                            "First occurrence unlocks stage 1."),
                                        new WheelPerkOccurrenceDraft(
                                            null,
                                            2,
                                            GetConvictionOccurrenceRequiredPoints(2),
                                            false,
                                            "Second occurrence unlocks stage 2.")
                                    ],
                                    stages));
                            }
                        }

                        continue;
                    }

                    string description = ExtractDescription(perkSection.Content);
                    if(string.IsNullOrWhiteSpace(description))
                    {
                        continue;
                    }

                    foreach(WheelVocation vocation in vocations)
                    {
                        drafts.Add(CreatePerkDraft(
                            vocation,
                            WheelPerkType.Conviction,
                            perkSection.Title,
                            description,
                            description,
                            article.Title,
                            article.WikiUrl,
                            vocations.Length > 1,
                            new
                            {
                                SourcePageTitle = article.Title,
                                SourceSectionTitle = section.Title,
                                SourceSubSectionTitle = perkSection.Title,
                                SourceKind = "wiki-page-section"
                            },
                            [
                                new WheelPerkOccurrenceDraft(
                                    null,
                                    1,
                                    GetConvictionOccurrenceRequiredPoints(1),
                                    true,
                                    "Conviction perks can appear on multiple slices and usually stack.")
                            ],
                            []));
                    }
                }
            }

            return drafts;
        }

        private IReadOnlyList<WheelPerkDraft> ParseRevelationPerks(
            IReadOnlyDictionary<string, WikiArticle> sourceArticles,
            ISet<string> usedSourceTitles)
        {
            WikiArticle article = sourceArticles["Wheel of Destiny/Revelation Perks"];
            usedSourceTitles.Add(article.Title);

            WikiSectionNode root = ParseSections(article.RawWikiText ?? string.Empty);
            List<WheelPerkDraft> drafts = [];

            foreach(WikiSectionNode section in root.Children)
            {
                if(!RevelationSectionVocationalMap.TryGetValue(section.Title, out WheelVocation[]? vocations))
                {
                    continue;
                }

                foreach(WikiSectionNode perkSection in section.Children)
                {
                    string summary = ExtractDescription(perkSection.Content);
                    if(string.IsNullOrWhiteSpace(summary))
                    {
                        continue;
                    }

                    foreach(WheelVocation vocation in ResolveRevelationVocations(perkSection.Title, vocations))
                    {
                        WikiArticle? dedicatedSource = ResolveDedicatedRevelationSource(
                            perkSection.Title,
                            vocation,
                            sourceArticles);

                        if(dedicatedSource is not null)
                        {
                            usedSourceTitles.Add(dedicatedSource.Title);
                        }

                        IReadOnlyList<WheelPerkStageDraft> stages = dedicatedSource is not null
                        ? ParseRevelationStagesFromSource(dedicatedSource.RawWikiText ?? string.Empty)
                        : ParseRevelationStagesFromSource(perkSection.Content);

                        string? description = ExtractSpellNotesDescription(dedicatedSource?.RawWikiText)
                                              ?? summary;

                        string? mainSourceTitle = dedicatedSource?.Title ?? article.Title;
                        string? mainSourceUrl = dedicatedSource?.WikiUrl ?? article.WikiUrl;

                        drafts.Add(CreatePerkDraft(
                            vocation,
                            WheelPerkType.Revelation,
                            perkSection.Title,
                            summary,
                            description,
                            mainSourceTitle,
                            mainSourceUrl,
                            ResolveRevelationVocations(perkSection.Title, vocations).Count > 1,
                            new
                            {
                                SourcePageTitle = article.Title,
                                SourceSectionTitle = section.Title,
                                SourceSubSectionTitle = perkSection.Title,
                                SourceKind = dedicatedSource is null ? "wiki-page-section" : "wiki-page-plus-spell-article",
                                StageSourceTitle = dedicatedSource?.Title
                            },
                            [
                                new WheelPerkOccurrenceDraft(
                                    null,
                                    1,
                                    RevelationMinimumDomainPoints,
                                    false,
                                    "Revelation perks start unlocking at 250 domain points.")
                            ],
                            stages));
                    }
                }
            }

            return drafts;
        }

        private static IReadOnlyList<WheelVocation> ResolveRevelationVocations(
            string perkName,
            IReadOnlyList<WheelVocation> defaultVocations)
        {
            if(string.Equals(perkName, "Avatar", StringComparison.OrdinalIgnoreCase))
            {
                return AllVocations;
            }

            if(string.Equals(perkName, "Gift of Life", StringComparison.OrdinalIgnoreCase))
            {
                return AllVocations;
            }

            return defaultVocations;
        }

        private static WikiArticle? ResolveDedicatedRevelationSource(
            string perkName,
            WheelVocation vocation,
            IReadOnlyDictionary<string, WikiArticle> sourceArticles)
        {
            if(string.Equals(perkName, "Avatar", StringComparison.OrdinalIgnoreCase) &&
               AvatarSourceTitleByVocation.TryGetValue(vocation, out string? avatarSourceTitle) &&
               sourceArticles.TryGetValue(avatarSourceTitle, out WikiArticle? avatarArticle))
            {
                return avatarArticle;
            }

            if(string.Equals(perkName, "Twin Bursts", StringComparison.OrdinalIgnoreCase))
            {
                if(sourceArticles.TryGetValue("Ice Burst", out WikiArticle? iceBurstArticle))
                {
                    return iceBurstArticle;
                }

                if(sourceArticles.TryGetValue("Terra Burst", out WikiArticle? terraBurstArticle))
                {
                    return terraBurstArticle;
                }
            }

            if(DedicatedRevelationSourceTitleByPerkName.TryGetValue(perkName, out string? sourceTitle) &&
               sourceArticles.TryGetValue(sourceTitle, out WikiArticle? article))
            {
                return article;
            }

            return null;
        }

        private static short GetConvictionOccurrenceRequiredPoints(short occurrenceIndex)
        {
            return (short)(ConvictionSliceMaxPoints * Math.Max(1, (int)occurrenceIndex));
        }

        private async Task SynchronizePlannerLayoutAsync(
            TibiaDbContext db,
            CancellationToken cancellationToken)
        {
            WheelPlannerLayoutSnapshot layoutSnapshot = await wheelPlannerLayoutSource.LoadAsync(cancellationToken);

            List<WheelPerk> perks = await db.WheelPerks
                                            .Include(entry => entry.Occurrences)
                                            .ToListAsync(cancellationToken);

            List<ResolvedWheelSectionDraft> resolvedSections = ResolvePlannerSections(layoutSnapshot.Sections, perks);
            List<ResolvedWheelRevelationSlotDraft> resolvedRevelationSlots = ResolvePlannerRevelationSlots(layoutSnapshot.RevelationSlots, perks);

            await SynchronizeSectionsAsync(db, resolvedSections, cancellationToken);
            await SynchronizeRevelationSlotsAsync(db, resolvedRevelationSlots, cancellationToken);

            logger.LogInformation(
                "Wheel planner layout synchronized with {SectionCount} sections and {SlotCount} revelation slots.",
                resolvedSections.Count,
                resolvedRevelationSlots.Count);
        }

        private static List<ResolvedWheelSectionDraft> ResolvePlannerSections(
            IReadOnlyList<WheelPlannerSectionSnapshot> sections,
            IReadOnlyList<WheelPerk> perks)
        {
            IReadOnlyDictionary<string, WheelPerk> perkLookup = BuildPerkLookup(perks);

            List<ResolvedWheelSectionDraft> resolvedSections = sections
                                                               .Select(section =>
                                                               {
                                                                   List<ResolvedWheelSectionDedicationDraft> dedicationPerks = ExtractDedicationPerkNames(section.DedicationText)
                                                                       .Select((name, index) => new ResolvedWheelSectionDedicationDraft(
                                                                           (short)(index + 1),
                                                                           ResolveRequiredPerk(
                                                                               perkLookup,
                                                                               section.Vocation,
                                                                               WheelPerkType.Dedication,
                                                                               name,
                                                                               $"section '{section.SectionKey}' dedication")))
                                                                       .ToList();

                                                                   if(dedicationPerks.Count == 0)
                                                                   {
                                                                       throw new InvalidOperationException($"Planner section '{section.SectionKey}' has no dedication perks.");
                                                                   }

                                                                   string convictionPerkName = ExtractConvictionPerkName(section.ConvictionText);
                                                                   WheelPerk convictionPerk = ResolveRequiredPerk(
                                                                       perkLookup,
                                                                       section.Vocation,
                                                                       WheelPerkType.Conviction,
                                                                       convictionPerkName,
                                                                       $"section '{section.SectionKey}' conviction");

                                                                   return new ResolvedWheelSectionDraft(
                                                                       section.Vocation,
                                                                       section.SectionKey,
                                                                       section.Quarter,
                                                                       section.RadiusIndex,
                                                                       section.SortOrder,
                                                                       section.SectionPoints,
                                                                       convictionPerk,
                                                                       dedicationPerks);
                                                               })
                                                               .OrderBy(entry => entry.Vocation)
                                                               .ThenBy(entry => entry.Quarter)
                                                               .ThenBy(entry => entry.SortOrder)
                                                               .ToList();

            foreach(IGrouping<int, ResolvedWheelSectionDraft> group in resolvedSections.GroupBy(entry => entry.ConvictionPerk.Id))
            {
                List<WheelPerkOccurrence> occurrences = group.First()
                                                            .ConvictionPerk
                                                            .Occurrences
                                                            .OrderBy(entry => entry.OccurrenceIndex)
                                                            .ToList();

                List<ResolvedWheelSectionDraft> orderedSections = group
                                                                  .OrderBy(entry => entry.SectionPoints)
                                                                  .ThenBy(entry => entry.Quarter)
                                                                  .ThenBy(entry => entry.SortOrder)
                                                                  .ThenBy(entry => entry.SectionKey, StringComparer.Ordinal)
                                                                  .ToList();

                if(occurrences.Count == 1 && orderedSections.Count == 1)
                {
                    orderedSections[0].ConvictionPerkOccurrence = occurrences[0];
                    continue;
                }

                if(occurrences.Count != orderedSections.Count)
                {
                    continue;
                }

                for (int index = 0; index < orderedSections.Count; index++)
                {
                    orderedSections[index].ConvictionPerkOccurrence = occurrences[index];
                }
            }

            return resolvedSections;
        }

        private static List<ResolvedWheelRevelationSlotDraft> ResolvePlannerRevelationSlots(
            IReadOnlyList<WheelPlannerRevelationSlotSnapshot> revelationSlots,
            IReadOnlyList<WheelPerk> perks)
        {
            IReadOnlyDictionary<string, WheelPerk> perkLookup = BuildPerkLookup(perks);

            List<ResolvedWheelRevelationSlotDraft> resolvedSlots = revelationSlots
                                                                   .Select(slot =>
                                                                   {
                                                                       string revelationPerkName = NormalizePlannerRevelationPerkName(slot.PerkName);
                                                                       WheelPerk perk = ResolveRequiredPerk(
                                                                           perkLookup,
                                                                           slot.Vocation,
                                                                           WheelPerkType.Revelation,
                                                                           revelationPerkName,
                                                                           $"revelation slot '{slot.SlotKey}'");

                                                                       return new ResolvedWheelRevelationSlotDraft(
                                                                           slot.Vocation,
                                                                           slot.SlotKey,
                                                                           slot.Quarter,
                                                                           slot.RequiredPoints,
                                                                           perk);
                                                                   })
                                                                   .OrderBy(entry => entry.Vocation)
                                                                   .ThenBy(entry => entry.Quarter)
                                                                   .ToList();

            foreach(IGrouping<int, ResolvedWheelRevelationSlotDraft> group in resolvedSlots.GroupBy(entry => entry.WheelPerk.Id))
            {
                List<WheelPerkOccurrence> occurrences = group.First()
                                                            .WheelPerk
                                                            .Occurrences
                                                            .OrderBy(entry => entry.OccurrenceIndex)
                                                            .ToList();

                List<ResolvedWheelRevelationSlotDraft> orderedSlots = group
                                                                      .OrderBy(entry => entry.RequiredPoints)
                                                                      .ThenBy(entry => entry.Quarter)
                                                                      .ThenBy(entry => entry.SlotKey, StringComparer.Ordinal)
                                                                      .ToList();

                if(occurrences.Count != orderedSlots.Count)
                {
                    continue;
                }

                for (int index = 0; index < orderedSlots.Count; index++)
                {
                    orderedSlots[index].WheelPerkOccurrence = occurrences[index];
                }
            }

            return resolvedSlots;
        }

        private static IReadOnlyDictionary<string, WheelPerk> BuildPerkLookup(IEnumerable<WheelPerk> perks)
        {
            Dictionary<string, WheelPerk> lookup = new(StringComparer.Ordinal);

            foreach(WheelPerk perk in perks)
            {
                string key = BuildPerkLookupKey(perk.Vocation, perk.Type, perk.Name);
                lookup.TryAdd(key, perk);
            }

            return lookup;
        }

        private static WheelPerk ResolveRequiredPerk(
            IReadOnlyDictionary<string, WheelPerk> perkLookup,
            WheelVocation vocation,
            WheelPerkType type,
            string perkName,
            string context)
        {
            foreach(string candidateName in ExpandPerkLookupCandidates(perkName))
            {
                string key = BuildPerkLookupKey(vocation, type, candidateName);

                if(perkLookup.TryGetValue(key, out WheelPerk? perk))
                {
                    return perk;
                }
            }

            throw new InvalidOperationException(
                $"Could not resolve {context} to an imported {type} perk for vocation '{vocation}' with name '{perkName}'.");
        }

        private static IEnumerable<string> ExpandPerkLookupCandidates(string perkName)
        {
            yield return perkName;

            if(PlannerPerkNameFallbacks.TryGetValue(perkName, out string[]? fallbacks))
            {
                foreach(string fallback in fallbacks)
                {
                    yield return fallback;
                }
            }
        }

        private static string BuildPerkLookupKey(
            WheelVocation vocation,
            WheelPerkType type,
            string perkName)
        {
            return $"{(int)vocation}:{(int)type}:{CreateSlug(CleanWikiText(perkName))}";
        }

        private async Task SynchronizeSectionsAsync(
            TibiaDbContext db,
            IReadOnlyList<ResolvedWheelSectionDraft> resolvedSections,
            CancellationToken cancellationToken)
        {
            List<WheelSection> existingSections = await db.WheelSections
                                                          .Include(entry => entry.DedicationPerks)
                                                          .ToListAsync(cancellationToken);

            Dictionary<string, WheelSection> existingByIdentity = existingSections.ToDictionary(
                entry => BuildSectionIdentity(entry.Vocation, entry.SectionKey),
                StringComparer.Ordinal);
            HashSet<string> incomingIdentities = resolvedSections
                                                 .Select(entry => BuildSectionIdentity(entry.Vocation, entry.SectionKey))
                                                 .ToHashSet(StringComparer.Ordinal);

            foreach(WheelSection removedSection in existingSections
                                                   .Where(entry => !incomingIdentities.Contains(BuildSectionIdentity(entry.Vocation, entry.SectionKey)))
                                                   .ToList())
            {
                db.WheelSections.Remove(removedSection);
            }

            foreach(ResolvedWheelSectionDraft section in resolvedSections)
            {
                string identity = BuildSectionIdentity(section.Vocation, section.SectionKey);

                if(!existingByIdentity.TryGetValue(identity, out WheelSection? trackedSection))
                {
                    db.WheelSections.Add(new WheelSection
                    {
                        Vocation = section.Vocation,
                        SectionKey = section.SectionKey,
                        Quarter = section.Quarter,
                        RadiusIndex = section.RadiusIndex,
                        SortOrder = section.SortOrder,
                        SectionPoints = section.SectionPoints,
                        ConvictionWheelPerkId = section.ConvictionPerk.Id,
                        ConvictionWheelPerkOccurrenceId = section.ConvictionPerkOccurrence?.Id,
                        DedicationPerks = section.DedicationPerks
                                                 .OrderBy(entry => entry.SortOrder)
                                                 .Select(entry => new WheelSectionDedicationPerk
                                                 {
                                                     WheelPerkId = entry.WheelPerk.Id,
                                                     SortOrder = entry.SortOrder
                                                 })
                                                 .ToList()
                    });
                    continue;
                }

                trackedSection.Quarter = section.Quarter;
                trackedSection.RadiusIndex = section.RadiusIndex;
                trackedSection.SortOrder = section.SortOrder;
                trackedSection.SectionPoints = section.SectionPoints;
                trackedSection.ConvictionWheelPerkId = section.ConvictionPerk.Id;
                trackedSection.ConvictionWheelPerkOccurrenceId = section.ConvictionPerkOccurrence?.Id;
                SynchronizeSectionDedicationPerks(trackedSection, section.DedicationPerks);
            }
        }

        private static void SynchronizeSectionDedicationPerks(
            WheelSection trackedSection,
            IReadOnlyList<ResolvedWheelSectionDedicationDraft> incomingDedicationPerks)
        {
            Dictionary<short, WheelSectionDedicationPerk> existingBySortOrder = trackedSection.DedicationPerks
                                                                                               .ToDictionary(
                                                                                                   entry => entry.SortOrder,
                                                                                                   entry => entry);
            HashSet<short> incomingSortOrders = incomingDedicationPerks
                                                .Select(entry => entry.SortOrder)
                                                .ToHashSet();

            foreach(WheelSectionDedicationPerk removedDedicationPerk in trackedSection.DedicationPerks
                                                                                      .Where(entry => !incomingSortOrders.Contains(entry.SortOrder))
                                                                                      .ToList())
            {
                trackedSection.DedicationPerks.Remove(removedDedicationPerk);
            }

            foreach(ResolvedWheelSectionDedicationDraft incomingDedicationPerk in incomingDedicationPerks.OrderBy(entry => entry.SortOrder))
            {
                if(!existingBySortOrder.TryGetValue(incomingDedicationPerk.SortOrder, out WheelSectionDedicationPerk? trackedDedicationPerk))
                {
                    trackedSection.DedicationPerks.Add(new WheelSectionDedicationPerk
                    {
                        WheelPerkId = incomingDedicationPerk.WheelPerk.Id,
                        SortOrder = incomingDedicationPerk.SortOrder
                    });
                    continue;
                }

                trackedDedicationPerk.WheelPerkId = incomingDedicationPerk.WheelPerk.Id;
            }
        }

        private async Task SynchronizeRevelationSlotsAsync(
            TibiaDbContext db,
            IReadOnlyList<ResolvedWheelRevelationSlotDraft> resolvedRevelationSlots,
            CancellationToken cancellationToken)
        {
            List<WheelRevelationSlot> existingSlots = await db.WheelRevelationSlots
                                                              .ToListAsync(cancellationToken);

            Dictionary<string, WheelRevelationSlot> existingByIdentity = existingSlots.ToDictionary(
                entry => BuildSlotIdentity(entry.Vocation, entry.SlotKey),
                StringComparer.Ordinal);
            HashSet<string> incomingIdentities = resolvedRevelationSlots
                                                 .Select(entry => BuildSlotIdentity(entry.Vocation, entry.SlotKey))
                                                 .ToHashSet(StringComparer.Ordinal);

            foreach(WheelRevelationSlot removedSlot in existingSlots
                                                         .Where(entry => !incomingIdentities.Contains(BuildSlotIdentity(entry.Vocation, entry.SlotKey)))
                                                         .ToList())
            {
                db.WheelRevelationSlots.Remove(removedSlot);
            }

            foreach(ResolvedWheelRevelationSlotDraft slot in resolvedRevelationSlots)
            {
                string identity = BuildSlotIdentity(slot.Vocation, slot.SlotKey);

                if(!existingByIdentity.TryGetValue(identity, out WheelRevelationSlot? trackedSlot))
                {
                    db.WheelRevelationSlots.Add(new WheelRevelationSlot
                    {
                        Vocation = slot.Vocation,
                        SlotKey = slot.SlotKey,
                        Quarter = slot.Quarter,
                        RequiredPoints = slot.RequiredPoints,
                        WheelPerkId = slot.WheelPerk.Id,
                        WheelPerkOccurrenceId = slot.WheelPerkOccurrence?.Id
                    });
                    continue;
                }

                trackedSlot.Quarter = slot.Quarter;
                trackedSlot.RequiredPoints = slot.RequiredPoints;
                trackedSlot.WheelPerkId = slot.WheelPerk.Id;
                trackedSlot.WheelPerkOccurrenceId = slot.WheelPerkOccurrence?.Id;
            }
        }

        private static string BuildSectionIdentity(
            WheelVocation vocation,
            string sectionKey)
        {
            return $"{(int)vocation}:{sectionKey}";
        }

        private static string BuildSlotIdentity(
            WheelVocation vocation,
            string slotKey)
        {
            return $"{(int)vocation}:{slotKey}";
        }

        private static List<string> ExtractDedicationPerkNames(string dedicationText)
        {
            List<string> names = dedicationText.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                               .Select(NormalizePlannerStatPerkName)
                                               .Where(entry => !string.IsNullOrWhiteSpace(entry))
                                               .Distinct(StringComparer.OrdinalIgnoreCase)
                                               .ToList()!;

            return names;
        }

        private static string ExtractConvictionPerkName(string convictionText)
        {
            string line = GetFirstNonEmptyLine(convictionText);

            if(line.StartsWith("Augmented ", StringComparison.OrdinalIgnoreCase))
            {
                return CleanWikiText(line["Augmented ".Length..]);
            }

            if(line.StartsWith("Vessel Resonance ", StringComparison.OrdinalIgnoreCase))
            {
                return "Vessel Resonance";
            }

            return NormalizePlannerStatPerkName(line)
                   ?? throw new InvalidOperationException("Planner conviction perk name is missing.");
        }

        private static string NormalizePlannerRevelationPerkName(string perkName)
        {
            string cleaned = CleanWikiText(perkName);

            if(cleaned.StartsWith("Avatar of ", StringComparison.OrdinalIgnoreCase))
            {
                return "Avatar";
            }

            return cleaned;
        }

        private static string GetFirstNonEmptyLine(string text)
        {
            return text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                       .FirstOrDefault()
                   ?? throw new InvalidOperationException("Planner text does not contain a readable first line.");
        }

        private static string? NormalizePlannerStatPerkName(string value)
        {
            string cleaned = CleanWikiText(value);
            if(string.IsNullOrWhiteSpace(cleaned))
            {
                return null;
            }

            string normalized = PlannerNumericPrefixRegex().Replace(cleaned, string.Empty);
            if(PlannerPerkNameAliases.TryGetValue(normalized, out string? alias))
            {
                normalized = alias;
            }

            return NormalizeOptionalText(normalized);
        }

        private static WheelPerkDraft CreatePerkDraft(
            WheelVocation vocation,
            WheelPerkType type,
            string name,
            string? summary,
            string? description,
            string? mainSourceTitle,
            string? mainSourceUrl,
            bool isGenericAcrossVocations,
            object metadata,
            IReadOnlyList<WheelPerkOccurrenceDraft> occurrences,
            IReadOnlyList<WheelPerkStageDraft> stages)
        {
            string cleanedName = Truncate(CleanWikiText(name), 255) ?? throw new InvalidOperationException("Wheel perk name is required.");
            string slug = CreateSlug(cleanedName);
            string key = Truncate($"{CreateSlug(MapVocationName(vocation))}:{type.ToString().ToLowerInvariant()}:{slug}", 191)
                         ?? throw new InvalidOperationException("Wheel perk key is required.");

            return new WheelPerkDraft(
                key,
                Truncate(slug, 128) ?? slug,
                vocation,
                type,
                cleanedName,
                Truncate(summary, 2000),
                description,
                Truncate(mainSourceTitle, 255),
                Truncate(mainSourceUrl, 500),
                isGenericAcrossVocations,
                JsonSerializer.Serialize(metadata),
                occurrences,
                stages);
        }

        private static IReadOnlyList<(string Name, string Description)> ParseBulletPerks(string rawText)
        {
            List<(string Name, string Description)> perks = [];

            foreach(string line in rawText.Split('\n'))
            {
                Match match = BulletPerkRegex().Match(line.Trim());
                if(!match.Success)
                {
                    continue;
                }

                string name = CleanWikiText(match.Groups["name"].Value);
                string description = CleanWikiText(match.Groups["description"].Value);

                if(string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                perks.Add((name, description));
            }

            return perks;
        }

        private static IReadOnlyList<(string Name, IReadOnlyList<WheelPerkStageDraft> Stages)> ParseAugmentationTable(string text)
        {
            List<IReadOnlyList<string>> rows = ParseFirstWikiTable(text);
            List<(string Name, IReadOnlyList<WheelPerkStageDraft> Stages)> results = [];

            foreach(IReadOnlyList<string> row in rows.Skip(1))
            {
                if(row.Count < 3)
                {
                    continue;
                }

                string name = CleanWikiText(row[0]);
                if(string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                List<WheelPerkStageDraft> stages =
                [
                    new(
                        1,
                        WheelStageUnlockKind.OccurrenceCount,
                        1,
                        Truncate(CleanWikiText(row[1]), 2000),
                        BuildStageDetailsJson(
                        [
                            new KeyValuePair<string, string>("effect", CleanWikiText(row[1]))
                        ]),
                        1),
                    new(
                        2,
                        WheelStageUnlockKind.OccurrenceCount,
                        2,
                        Truncate(CleanWikiText(row[2]), 2000),
                        BuildStageDetailsJson(
                        [
                            new KeyValuePair<string, string>("effect", CleanWikiText(row[2]))
                        ]),
                        2)
                ];

                results.Add((name, stages));
            }

            return results;
        }

        private static IReadOnlyList<WheelPerkStageDraft> ParseRevelationStagesFromSource(string rawText)
        {
            List<IReadOnlyList<string>> rows = ParseFirstWikiTable(rawText);
            if(rows.Count > 0)
            {
                IReadOnlyList<string> headerRow = rows[0];
                List<int> stages = headerRow
                                   .Skip(1)
                                   .Select(ParseStageNumber)
                                   .Where(entry => entry > 0)
                                   .ToList();

                if(stages.Count > 0)
                {
                    Dictionary<int, List<KeyValuePair<string, string>>> details = stages.ToDictionary(entry => entry, _ => new List<KeyValuePair<string, string>>());

                    foreach(IReadOnlyList<string> row in rows.Skip(1))
                    {
                        if(row.Count < 2)
                        {
                            continue;
                        }

                        string effectName = CleanWikiText(row[0]);
                        if(string.IsNullOrWhiteSpace(effectName))
                        {
                            continue;
                        }

                        IReadOnlyList<string> values = ExpandStageValues(row.Skip(1).ToList(), stages.Count);

                        for (int index = 0; index < stages.Count && index < values.Count; index++)
                        {
                            string value = CleanWikiText(values[index]);

                            if(string.IsNullOrWhiteSpace(value))
                            {
                                continue;
                            }

                            details[stages[index]].Add(new KeyValuePair<string, string>(effectName, value));
                        }
                    }

                    return BuildRevelationStageDrafts(details);
                }
            }

            return ParseRevelationStagesFromInlineText(rawText);
        }

        private static IReadOnlyList<WheelPerkStageDraft> ParseRevelationStagesFromInlineText(string rawText)
        {
            string cleaned = CleanWikiText(rawText);
            MatchCollection matches = InlineStageTripletRegex().Matches(cleaned);

            if(matches.Count == 0)
            {
                return [];
            }

            Dictionary<int, List<KeyValuePair<string, string>>> details = new()
            {
                [1] = [],
                [2] = [],
                [3] = []
            };

            for (int index = 0; index < matches.Count; index++)
            {
                Match match = matches[index];
                int nextMatchIndex = index < matches.Count - 1 ? matches[index + 1].Index : -1;
                string effectName = ExtractInlineStageEffectName(cleaned, match, nextMatchIndex);

                if(string.IsNullOrWhiteSpace(effectName))
                {
                    continue;
                }

                details[1].Add(new KeyValuePair<string, string>(effectName, match.Groups["value1"].Value));
                details[2].Add(new KeyValuePair<string, string>(effectName, match.Groups["value2"].Value));
                details[3].Add(new KeyValuePair<string, string>(effectName, match.Groups["value3"].Value));
            }

            return BuildRevelationStageDrafts(details);
        }

        private static IReadOnlyList<WheelPerkStageDraft> BuildRevelationStageDrafts(
            IReadOnlyDictionary<int, List<KeyValuePair<string, string>>> detailsByStage)
        {
            return detailsByStage.Keys
                                 .OrderBy(entry => entry)
                                 .Select(entry => new WheelPerkStageDraft(
                                     (byte)entry,
                                     WheelStageUnlockKind.DomainPoints,
                                     (short)(entry switch
                                     {
                                         1 => 250,
                                         2 => 500,
                                         3 => 1000,
                                         _ => entry * 250
                                     }),
                                     Truncate(string.Join("; ", detailsByStage[entry].Select(pair => $"{pair.Key}: {pair.Value}")), 2000),
                                     BuildStageDetailsJson(detailsByStage[entry]),
                                     (short)entry))
                                 .ToList();
        }

        private static IReadOnlyList<string> ExpandStageValues(IReadOnlyList<string> values, int stageCount)
        {
            if(values.Count == stageCount)
            {
                return values;
            }

            if(values.Count == 1)
            {
                return Enumerable.Repeat(values[0], stageCount).ToList();
            }

            if(values.Count > stageCount)
            {
                return values.Take(stageCount).ToList();
            }

            List<string> expanded = values.ToList();

            while(expanded.Count < stageCount)
            {
                expanded.Add(values.Last());
            }

            return expanded;
        }

        private static string? ExtractSpellNotesDescription(string? rawText)
        {
            if(string.IsNullOrWhiteSpace(rawText))
            {
                return null;
            }

            Dictionary<string, string> parameters = ParseInfoboxParameters(rawText);

            if(!parameters.TryGetValue("notes", out string? notes) || string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            string normalizedNotes = notes;
            normalizedNotes = NormalizeTableSyntax(normalizedNotes);
            int tableStartIndex = normalizedNotes.IndexOf("{|", StringComparison.Ordinal);

            if(tableStartIndex >= 0)
            {
                normalizedNotes = normalizedNotes[..tableStartIndex];
            }

            return NormalizeOptionalText(CleanWikiText(normalizedNotes));
        }

        private static Dictionary<string, string> ParseInfoboxParameters(string rawText)
        {
            string? infoboxTemplate = ExtractFirstTemplate(rawText, "Infobox Spell");
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
                bool isClosingLine = templateDepth == 1 && trimmedLine.StartsWith("}}", StringComparison.Ordinal);
                bool isTopLevelParameter = templateDepth == 1 && !isClosingLine && IsTopLevelParameterLine(trimmedLine);

                if(isTopLevelParameter)
                {
                    FlushCurrentValue(values, currentKey, currentValue);

                    int equalsIndex = trimmedLine.IndexOf('=');
                    currentKey = CleanWikiText(trimmedLine[1..equalsIndex]).Replace(" ", string.Empty).ToLowerInvariant();
                    currentValue = new StringBuilder();
                    AppendValueLine(currentValue, trimmedLine[(equalsIndex + 1)..]);
                }
                else if(currentKey is not null && !isClosingLine && templateDepth >= 1)
                {
                    AppendValueLine(currentValue!, line);
                }

                templateDepth += CountOccurrences(line, "{{");
                templateDepth -= CountOccurrences(line, "}}");
            }

            FlushCurrentValue(values, currentKey, currentValue);

            return values;
        }

        private static string? ExtractFirstTemplate(string rawText, string templateName)
        {
            Match match = Regex.Match(
                rawText,
                @"\{\{\s*" + Regex.Escape(templateName) + @"\b",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if(!match.Success)
            {
                return null;
            }

            int startIndex = match.Index;
            int depth = 0;

            for (int index = startIndex; index < rawText.Length - 1; index++)
            {
                if(rawText[index] == '{' && rawText[index + 1] == '{')
                {
                    depth++;
                    index++;
                    continue;
                }

                if(rawText[index] == '}' && rawText[index + 1] == '}')
                {
                    depth--;
                    index++;

                    if(depth == 0)
                    {
                        return rawText[startIndex..(index + 1)];
                    }
                }
            }

            return null;
        }

        private static bool IsTopLevelParameterLine(string line)
        {
            if(!line.StartsWith("|", StringComparison.Ordinal))
            {
                return false;
            }

            int equalsIndex = line.IndexOf('=');
            return equalsIndex > 1;
        }

        private static void AppendValueLine(StringBuilder builder, string line)
        {
            if(builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(line);
        }

        private static void FlushCurrentValue(
            IDictionary<string, string> values,
            string? key,
            StringBuilder? builder)
        {
            if(string.IsNullOrWhiteSpace(key) || builder is null)
            {
                return;
            }

            string value = builder.ToString().Trim();
            if(string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            values[key] = value;
        }

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;

            while((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }

        private static WikiSectionNode ParseSections(string rawText)
        {
            WikiSectionNode root = new(0, string.Empty);
            Stack<WikiSectionNode> stack = new();
            stack.Push(root);

            foreach(string originalLine in rawText.Split('\n'))
            {
                string line = originalLine.TrimEnd('\r');
                Match headingMatch = HeadingRegex().Match(line.Trim());

                if(headingMatch.Success)
                {
                    int level = headingMatch.Groups["markers"].Value.Length;
                    string title = CleanHeadingTitle(headingMatch.Groups["title"].Value);
                    WikiSectionNode node = new(level, title);

                    while(stack.Peek().Level >= level)
                    {
                        stack.Pop();
                    }

                    stack.Peek().Children.Add(node);
                    stack.Push(node);
                    continue;
                }

                stack.Peek().Lines.Add(line);
            }

            return root;
        }

        private static string ExtractDescription(string text)
        {
            List<string> lines = [];

            foreach(string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();

                if(string.IsNullOrWhiteSpace(line))
                {
                    if(lines.Count > 0)
                    {
                        lines.Add(string.Empty);
                    }

                    continue;
                }

                if(line.StartsWith("{|", StringComparison.Ordinal) || line.StartsWith("{{{!}}", StringComparison.Ordinal))
                {
                    break;
                }

                if(line.StartsWith("{{mainarticle", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("{{seealso", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("{{see also", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                lines.Add(line);
            }

            return CleanWikiText(string.Join(" ", lines));
        }

        private static List<IReadOnlyList<string>> ParseFirstWikiTable(string text)
        {
            string normalizedText = NormalizeTableSyntax(text);
            Match match = WikiTableRegex().Match(normalizedText);

            if(!match.Success)
            {
                return [];
            }

            string table = match.Value;
            List<List<string>> rows = [];
            List<string> currentRowLines = [];

            foreach(string rawLine in table.Split('\n'))
            {
                string line = rawLine.Trim();

                if(line.StartsWith("{|", StringComparison.Ordinal) || line.StartsWith("|}", StringComparison.Ordinal))
                {
                    continue;
                }

                if(line.StartsWith("|-", StringComparison.Ordinal))
                {
                    if(currentRowLines.Count > 0)
                    {
                        rows.Add(ParseTableRow(currentRowLines));
                        currentRowLines = [];
                    }

                    continue;
                }

                if(!string.IsNullOrWhiteSpace(line))
                {
                    currentRowLines.Add(line);
                }
            }

            if(currentRowLines.Count > 0)
            {
                rows.Add(ParseTableRow(currentRowLines));
            }

            return rows
                   .Where(entry => entry.Count > 0)
                   .Cast<IReadOnlyList<string>>()
                   .ToList();
        }

        private static List<string> ParseTableRow(IReadOnlyList<string> rowLines)
        {
            List<string> cells = [];

            foreach(string rowLine in rowLines)
            {
                if(rowLine.StartsWith("!", StringComparison.Ordinal))
                {
                    cells.AddRange(ParseHeaderCells(rowLine));
                    continue;
                }

                if(rowLine.StartsWith("|", StringComparison.Ordinal))
                {
                    cells.AddRange(ParseDataCells(rowLine));
                }
            }

            return cells
                   .Select(CleanWikiText)
                   .Where(value => !string.IsNullOrWhiteSpace(value))
                   .ToList();
        }

        private static IEnumerable<string> ParseHeaderCells(string rowLine)
        {
            string content = rowLine[1..];
            return content.Split("!!", StringSplitOptions.None)
                          .Select(ExtractCellValue);
        }

        private static IEnumerable<string> ParseDataCells(string rowLine)
        {
            string content = rowLine[1..];
            string[] parts = content.Split("||", StringSplitOptions.None);

            foreach(string part in parts)
            {
                yield return ExtractCellValue(part);
            }
        }

        private static string ExtractCellValue(string rawCell)
        {
            string cell = rawCell.Trim();
            int pipeIndex = FindTopLevelPipeIndex(cell);

            if(pipeIndex >= 0)
            {
                string trailingValue = cell[(pipeIndex + 1)..].Trim();

                if(!string.IsNullOrWhiteSpace(trailingValue))
                {
                    return trailingValue;
                }
            }

            return cell;
        }

        private static int FindTopLevelPipeIndex(string value)
        {
            int wikiLinkDepth = 0;
            int templateDepth = 0;

            for (int index = 0; index < value.Length; index++)
            {
                if(index < value.Length - 1)
                {
                    if(value[index] == '[' && value[index + 1] == '[')
                    {
                        wikiLinkDepth++;
                        index++;
                        continue;
                    }

                    if(value[index] == ']' && value[index + 1] == ']')
                    {
                        wikiLinkDepth = Math.Max(0, wikiLinkDepth - 1);
                        index++;
                        continue;
                    }

                    if(value[index] == '{' && value[index + 1] == '{')
                    {
                        templateDepth++;
                        index++;
                        continue;
                    }

                    if(value[index] == '}' && value[index + 1] == '}')
                    {
                        templateDepth = Math.Max(0, templateDepth - 1);
                        index++;
                        continue;
                    }
                }

                if(value[index] == '|' && wikiLinkDepth == 0 && templateDepth == 0)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int ParseStageNumber(string value)
        {
            Match match = StageNumberRegex().Match(value);
            return match.Success && int.TryParse(match.Groups["value"].Value, out int stage) ? stage : 0;
        }

        private static string ExtractInlineStageEffectName(string cleanedText, Match valueMatch, int nextMatchIndex)
        {
            int segmentStart = FindLastSentenceBoundary(cleanedText, valueMatch.Index);
            string prefix = cleanedText[segmentStart..valueMatch.Index].Trim();

            int conjunctionIndex = prefix.LastIndexOf(" and ", StringComparison.OrdinalIgnoreCase);
            if(conjunctionIndex >= 0)
            {
                prefix = prefix[(conjunctionIndex + " and ".Length)..].Trim();
            }

            prefix = TrimTrailingConnector(prefix);

            int suffixEnd = nextMatchIndex >= 0
                ? nextMatchIndex
                : FindNextSentenceBoundary(cleanedText, valueMatch.Index + valueMatch.Length);

            string suffix = suffixEnd > valueMatch.Index + valueMatch.Length
                ? cleanedText[(valueMatch.Index + valueMatch.Length)..suffixEnd].Trim()
                : string.Empty;

            return ShouldAppendInlineSuffix(suffix)
                ? CleanWikiText($"{prefix} {suffix}")
                : CleanWikiText(prefix);
        }

        private static int FindLastSentenceBoundary(string text, int index)
        {
            int lastPeriod = text.LastIndexOf('.', Math.Max(0, index - 1));
            int lastSemicolon = text.LastIndexOf(';', Math.Max(0, index - 1));
            int lastColon = text.LastIndexOf(':', Math.Max(0, index - 1));
            int boundary = Math.Max(lastPeriod, Math.Max(lastSemicolon, lastColon));
            return boundary < 0 ? 0 : boundary + 1;
        }

        private static int FindNextSentenceBoundary(string text, int startIndex)
        {
            int periodIndex = text.IndexOf('.', startIndex);
            int semicolonIndex = text.IndexOf(';', startIndex);
            int colonIndex = text.IndexOf(':', startIndex);

            int[] indexes = [periodIndex, semicolonIndex, colonIndex];
            return indexes.Where(entry => entry >= 0).DefaultIfEmpty(text.Length).Min();
        }

        private static string TrimTrailingConnector(string value)
        {
            string trimmed = value.Trim();
            string[] suffixes =
            [
                " equal to",
                " equals",
                " equal",
                " by",
                " to"
            ];

            foreach(string suffix in suffixes)
            {
                if(trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed[..^suffix.Length].Trim();
                }
            }

            return trimmed;
        }

        private static bool ShouldAppendInlineSuffix(string suffix)
        {
            if(string.IsNullOrWhiteSpace(suffix))
            {
                return false;
            }

            return suffix.StartsWith("of ", StringComparison.OrdinalIgnoreCase) ||
                   suffix.StartsWith("per ", StringComparison.OrdinalIgnoreCase) ||
                   suffix.StartsWith("for ", StringComparison.OrdinalIgnoreCase) ||
                   suffix.StartsWith("while ", StringComparison.OrdinalIgnoreCase) ||
                   suffix.StartsWith("when ", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeTableSyntax(string text)
        {
            return text.Replace("{{{!}}", "{|", StringComparison.Ordinal)
                       .Replace("{{!}}", "|", StringComparison.Ordinal);
        }

        private static string CleanHeadingTitle(string rawTitle)
        {
            return NormalizeOptionalText(CleanWikiText(rawTitle)) ?? string.Empty;
        }

        private static string CleanWikiText(string? rawText)
        {
            if(string.IsNullOrWhiteSpace(rawText))
            {
                return string.Empty;
            }

            string cleaned = NormalizeTableSyntax(rawText);
            cleaned = FileLinkRegex().Replace(cleaned, string.Empty);
            cleaned = WikiLinkRegex().Replace(cleaned, match => match.Groups["label"].Success
                ? match.Groups["label"].Value
                : match.Groups["target"].Value);
            cleaned = SimpleTemplateRegex().Replace(cleaned, string.Empty);
            cleaned = cleaned.Replace("'''", string.Empty, StringComparison.Ordinal)
                             .Replace("''", string.Empty, StringComparison.Ordinal);
            cleaned = BreakRegex().Replace(cleaned, "; ");
            cleaned = HtmlTagRegex().Replace(cleaned, " ");
            cleaned = WebUtility.HtmlDecode(cleaned);
            cleaned = cleaned.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
                             .Replace("\r", " ", StringComparison.Ordinal)
                             .Replace("\n", " ", StringComparison.Ordinal);

            return WhitespaceRegex().Replace(cleaned, " ").Trim(' ', ':', ';');
        }

        private static string? NormalizeOptionalText(string? value)
        {
            string cleaned = CleanWikiText(value);
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        private static string CreateSlug(string value)
        {
            string normalized = EntityNameNormalizer.Normalize(value);
            string slug = SlugNonAlphaNumericRegex().Replace(normalized, "-");
            slug = slug.Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? "wheel-perk" : slug;
        }

        private static string MapVocationName(WheelVocation vocation)
        {
            return vocation switch
            {
                WheelVocation.EliteKnight => "Elite Knight",
                WheelVocation.RoyalPaladin => "Royal Paladin",
                WheelVocation.ElderDruid => "Elder Druid",
                WheelVocation.MasterSorcerer => "Master Sorcerer",
                WheelVocation.ExaltedMonk => "Exalted Monk",
                _ => vocation.ToString()
            };
        }

        private static string? BuildStageDetailsJson(IEnumerable<KeyValuePair<string, string>> details)
        {
            List<object> items = details
                                 .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
                                 .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
                                 .Select(entry => new
                                 {
                                     Name = entry.Key,
                                     Value = entry.Value
                                 })
                                 .Cast<object>()
                                 .ToList();

            return items.Count == 0 ? null : JsonSerializer.Serialize(items);
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }

        [GeneratedRegex(@"^\*\s*(?:\[\[File:[^\]]+\]\]\s*)?'''(?<name>[^']+)''':\s*(?<description>.+)$")]
        private static partial Regex BulletPerkRegex();

        [GeneratedRegex(@"^(?<markers>={2,6})\s*(?<title>.*?)\s*\k<markers>$")]
        private static partial Regex HeadingRegex();

        [GeneratedRegex(@"\{\|.*?\|\}", RegexOptions.Singleline)]
        private static partial Regex WikiTableRegex();

        [GeneratedRegex(@"Stage\s+(?<value>\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex StageNumberRegex();

        [GeneratedRegex(@"(?<value1>\d+(?:\.\d+)?%?)\s*/\s*(?<value2>\d+(?:\.\d+)?%?)\s*/\s*(?<value3>\d+(?:\.\d+)?%?)")]
        private static partial Regex InlineStageTripletRegex();

        [GeneratedRegex(@"\[\[(?:File|Image):[^\]]+\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex FileLinkRegex();

        [GeneratedRegex(@"\[\[(?<target>[^\]|]+)(?:\|(?<label>[^\]]+))?\]\]")]
        private static partial Regex WikiLinkRegex();

        [GeneratedRegex(@"\{\{[^{}]+\}\}")]
        private static partial Regex SimpleTemplateRegex();

        [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
        private static partial Regex BreakRegex();

        [GeneratedRegex(@"<[^>]+>")]
        private static partial Regex HtmlTagRegex();

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        [GeneratedRegex(@"[^a-z0-9]+")]
        private static partial Regex SlugNonAlphaNumericRegex();

        [GeneratedRegex(@"^[+-]?\d+(?:\.\d+)?%?\s+")]
        private static partial Regex PlannerNumericPrefixRegex();

        private sealed record WheelPerkDraft(
            string Key,
            string Slug,
            WheelVocation Vocation,
            WheelPerkType Type,
            string Name,
            string? Summary,
            string? Description,
            string? MainSourceTitle,
            string? MainSourceUrl,
            bool IsGenericAcrossVocations,
            string? MetadataJson,
            IReadOnlyList<WheelPerkOccurrenceDraft> Occurrences,
            IReadOnlyList<WheelPerkStageDraft> Stages);

        private sealed record WheelPerkOccurrenceDraft(
            byte? Domain,
            short OccurrenceIndex,
            short? RequiredPoints,
            bool IsStackable,
            string? Notes);

        private sealed record WheelPerkStageDraft(
            byte Stage,
            WheelStageUnlockKind UnlockKind,
            short UnlockValue,
            string? EffectSummary,
            string? EffectDetailsJson,
            short SortOrder);

        private sealed record ResolvedWheelSectionDedicationDraft(
            short SortOrder,
            WheelPerk WheelPerk);

        private sealed class ResolvedWheelSectionDraft(
            WheelVocation vocation,
            string sectionKey,
            WheelQuarter quarter,
            byte radiusIndex,
            short sortOrder,
            short sectionPoints,
            WheelPerk convictionPerk,
            IReadOnlyList<ResolvedWheelSectionDedicationDraft> dedicationPerks)
        {
            public WheelVocation Vocation { get; } = vocation;

            public string SectionKey { get; } = sectionKey;

            public WheelQuarter Quarter { get; } = quarter;

            public byte RadiusIndex { get; } = radiusIndex;

            public short SortOrder { get; } = sortOrder;

            public short SectionPoints { get; } = sectionPoints;

            public WheelPerk ConvictionPerk { get; } = convictionPerk;

            public IReadOnlyList<ResolvedWheelSectionDedicationDraft> DedicationPerks { get; } = dedicationPerks;

            public WheelPerkOccurrence? ConvictionPerkOccurrence { get; set; }
        }

        private sealed class ResolvedWheelRevelationSlotDraft(
            WheelVocation vocation,
            string slotKey,
            WheelQuarter quarter,
            short requiredPoints,
            WheelPerk wheelPerk)
        {
            public WheelVocation Vocation { get; } = vocation;

            public string SlotKey { get; } = slotKey;

            public WheelQuarter Quarter { get; } = quarter;

            public short RequiredPoints { get; } = requiredPoints;

            public WheelPerk WheelPerk { get; } = wheelPerk;

            public WheelPerkOccurrence? WheelPerkOccurrence { get; set; }
        }

        private sealed class WikiSectionNode(int level, string title)
        {
            public int Level { get; } = level;

            public string Title { get; } = title;

            public List<string> Lines { get; } = [];

            public List<WikiSectionNode> Children { get; } = [];

            public string Content => string.Join('\n', Lines);
        }
    }
}
