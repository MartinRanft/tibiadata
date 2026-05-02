using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Entities.WheelOfDestiny;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper.Implementations;
using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.WheelOfDestiny
{
        public sealed class HybridGemModDataImportService(
        IWheelPlannerLayoutSource plannerSource,
        BasicModTableScraper basicModScraper,
        SupremeModTableScraper supremeModScraper,
        ILogger<HybridGemModDataImportService> logger) : IGemModDataImportService
    {
        public async Task<GemImportResult> ImportGemsAsync(TibiaDbContext db, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (db.Database.CurrentTransaction is not null)
            {
                logger.LogDebug("Reusing existing database transaction for hybrid gem/mod import.");
                return await ImportGemsCoreAsync(db, cancellationToken);
            }

            IExecutionStrategy executionStrategy = db.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction? transaction = db.Database.IsRelational()
                    ? await db.Database.BeginTransactionAsync(cancellationToken)
                    : null;

                GemImportResult result = await ImportGemsCoreAsync(db, cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return result;
            });
        }

        private async Task<GemImportResult> ImportGemsCoreAsync(TibiaDbContext db, CancellationToken cancellationToken)
        {
            WheelPlannerFullSnapshot plannerSnapshot = await plannerSource.LoadFullAsync(cancellationToken);
            List<ModDraft> wikiBasicMods = await basicModScraper.ScrapeAsync(cancellationToken);
            List<ModDraft> wikiSupremeMods = await supremeModScraper.ScrapeAsync(cancellationToken);

            List<GemDraft> gemDrafts = MergeGemData(plannerSnapshot.Gems);
            List<ModifierDraft> modDrafts = MergeModifierData(plannerSnapshot.Mods, wikiBasicMods, wikiSupremeMods);

            ValidateMergedData(gemDrafts, modDrafts, plannerSnapshot, wikiBasicMods, wikiSupremeMods);

            GemImportResult gemResult = await PersistGemsAsync(db, gemDrafts, cancellationToken);
            ModImportResult modResult = await PersistModifiersAsync(db, modDrafts, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Hybrid gem/mod import completed. Gems: {GemCount}, Mods: {ModCount} (Basic: {BasicCount}, Supreme: {SupremeCount})",
                gemResult.GemsProcessed,
                modResult.TotalProcessed,
                modResult.BasicProcessed,
                modResult.SupremeProcessed);

            return new GemImportResult(
                SourcePageCount: gemResult.SourcePageCount + (wikiBasicMods.Count > 0 || wikiSupremeMods.Count > 0 ? 2 : 0),
                GemsProcessed: gemResult.GemsProcessed,
                ModifiersProcessed: modResult.TotalProcessed,
                Added: gemResult.Added + modResult.Added,
                Updated: gemResult.Updated + modResult.Updated,
                Unchanged: gemResult.Unchanged + modResult.Unchanged,
                Removed: gemResult.Removed + modResult.Removed);
        }

        private List<GemDraft> MergeGemData(List<WheelPlannerGemSnapshot> plannerGems)
        {
            if (plannerGems.Count > 0)
            {
                logger.LogInformation("Using {Count} gems from the official Wheel Planner", plannerGems.Count);

                return plannerGems
                    .Select(gem => new GemDraft(
                        Name: gem.Name,
                        WikiUrl: $"https://tibia.fandom.com/wiki/{gem.Name.Replace(" ", "_", StringComparison.Ordinal)}",
                        GemFamily: gem.Family,
                        GemSize: gem.Size,
                        VocationRestriction: gem.VocationRestriction,
                        Description: null))
                    .OrderBy(gem => gem.Name, StringComparer.Ordinal)
                    .ToList();
            }

            logger.LogWarning("Wheel Planner returned no gem data. Falling back to the static gem catalog.");
            return CreateStaticGemCatalog();
        }

        private static List<GemDraft> CreateStaticGemCatalog()
        {
            return
            [
                CreateFallbackGem("Guardian Gem", GemFamily.Guardian, GemSize.Regular, GemVocation.Knight),
                CreateFallbackGem("Lesser Guardian Gem", GemFamily.Guardian, GemSize.Lesser, GemVocation.Knight),
                CreateFallbackGem("Greater Guardian Gem", GemFamily.Guardian, GemSize.Greater, GemVocation.Knight),
                CreateFallbackGem("Marksman Gem", GemFamily.Marksman, GemSize.Regular, GemVocation.Paladin),
                CreateFallbackGem("Lesser Marksman Gem", GemFamily.Marksman, GemSize.Lesser, GemVocation.Paladin),
                CreateFallbackGem("Greater Marksman Gem", GemFamily.Marksman, GemSize.Greater, GemVocation.Paladin),
                CreateFallbackGem("Mystic Gem", GemFamily.Mystic, GemSize.Regular, GemVocation.Druid),
                CreateFallbackGem("Lesser Mystic Gem", GemFamily.Mystic, GemSize.Lesser, GemVocation.Druid),
                CreateFallbackGem("Greater Mystic Gem", GemFamily.Mystic, GemSize.Greater, GemVocation.Druid),
                CreateFallbackGem("Sage Gem", GemFamily.Sage, GemSize.Regular, GemVocation.Sorcerer),
                CreateFallbackGem("Lesser Sage Gem", GemFamily.Sage, GemSize.Lesser, GemVocation.Sorcerer),
                CreateFallbackGem("Greater Sage Gem", GemFamily.Sage, GemSize.Greater, GemVocation.Sorcerer),
                CreateFallbackGem("Spiritualist Gem", GemFamily.Spiritualist, GemSize.Regular, GemVocation.Monk),
                CreateFallbackGem("Lesser Spiritualist Gem", GemFamily.Spiritualist, GemSize.Lesser, GemVocation.Monk),
                CreateFallbackGem("Greater Spiritualist Gem", GemFamily.Spiritualist, GemSize.Greater, GemVocation.Monk)
            ];
        }

        private static GemDraft CreateFallbackGem(string name, GemFamily family, GemSize size, GemVocation vocation)
        {
            return new GemDraft(
                Name: name,
                WikiUrl: $"https://tibia.fandom.com/wiki/{name.Replace(" ", "_", StringComparison.Ordinal)}",
                GemFamily: family,
                GemSize: size,
                VocationRestriction: vocation,
                Description: null);
        }

        private List<ModifierDraft> MergeModifierData(
            List<WheelPlannerModSnapshot> plannerMods,
            List<ModDraft> wikiBasic,
            List<ModDraft> wikiSupreme)
        {
            List<ModDraft> wikiMods = wikiBasic.Concat(wikiSupreme).ToList();
            List<ModifierDraft> plannerDrafts = plannerMods.Select(ConvertPlannerDraft).ToList();

            if (wikiMods.Count == 0)
            {
                logger.LogWarning("No TibiaWiki modifier tables were available. Using official planner modifiers only.");
                return plannerDrafts;
            }

            Dictionary<string, List<ModifierDraft>> plannerByNameType = plannerDrafts
                .GroupBy(CreateNameTypeKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            List<ModifierDraft> merged = [];

            foreach (ModDraft wikiMod in wikiMods)
            {
                ModifierDraft baseDraft = ConvertWikiDraft(wikiMod);
                string nameTypeKey = CreateNameTypeKey(baseDraft);
                plannerByNameType.TryGetValue(nameTypeKey, out List<ModifierDraft>? candidates);

                ModifierDraft? matchedPlanner = MatchPlannerCandidate(baseDraft, candidates ?? []);
                merged.Add(matchedPlanner is null
                    ? baseDraft
                    : ApplyPlannerOverride(baseDraft, matchedPlanner));
            }

            foreach (ModifierDraft plannerDraft in plannerDrafts)
            {
                bool coveredByWiki = merged.Any(existing => MatchesPlannerCoverage(existing, plannerDraft));
                if (!coveredByWiki)
                {
                    merged.Add(plannerDraft);
                }
            }

            return merged
                .OrderBy(entry => entry.Type)
                .ThenBy(entry => entry.Category)
                .ThenBy(entry => entry.VocationRestriction)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .ThenBy(entry => entry.VariantKey, StringComparer.Ordinal)
                .ToList();
        }

        private static ModifierDraft ConvertWikiDraft(ModDraft draft)
        {
            return new ModifierDraft(
                VariantKey: draft.VariantKey,
                Name: draft.Name,
                Type: draft.Type,
                Category: draft.Category,
                VocationRestriction: draft.VocationRestriction,
                GradeValues: new Dictionary<GemGrade, string>(draft.GradeValues),
                IsCombo: draft.IsCombo,
                HasTradeoff: draft.HasTradeoff,
                Description: draft.Description);
        }

        private static ModifierDraft ConvertPlannerDraft(WheelPlannerModSnapshot draft)
        {
            return new ModifierDraft(
                VariantKey: draft.VariantKey,
                Name: draft.Name,
                Type: draft.Type,
                Category: draft.Category,
                VocationRestriction: draft.VocationRestriction,
                GradeValues: new Dictionary<GemGrade, string>(draft.GradeValues),
                IsCombo: DetectComboMod(draft.Name),
                HasTradeoff: DetectTradeoffMod(draft.GradeValues),
                Description: draft.Description);
        }

        private static ModifierDraft? MatchPlannerCandidate(ModifierDraft wikiDraft, IReadOnlyList<ModifierDraft> candidates)
        {
            if (candidates.Count == 0)
            {
                return null;
            }

            List<ModifierDraft> vocationMatches = candidates
                .Where(candidate => candidate.VocationRestriction == wikiDraft.VocationRestriction)
                .ToList();

            if (vocationMatches.Count == 1)
            {
                return vocationMatches[0];
            }

            if (vocationMatches.Count > 1)
            {
                ModifierDraft? exactGrades = vocationMatches.FirstOrDefault(candidate => GradeValuesMatchForKnownWikiValues(candidate.GradeValues, wikiDraft.GradeValues));
                return exactGrades ?? vocationMatches[0];
            }

            List<ModifierDraft> sharedMatches = candidates
                .Where(candidate => candidate.VocationRestriction is null)
                .ToList();

            if (sharedMatches.Count == 1)
            {
                return sharedMatches[0];
            }

            if (sharedMatches.Count > 1)
            {
                ModifierDraft? exactGrades = sharedMatches.FirstOrDefault(candidate => GradeValuesMatchForKnownWikiValues(candidate.GradeValues, wikiDraft.GradeValues));
                return exactGrades ?? sharedMatches[0];
            }

            return candidates[0];
        }

        private static bool GradeValuesMatchForKnownWikiValues(
            IReadOnlyDictionary<GemGrade, string> plannerValues,
            IReadOnlyDictionary<GemGrade, string> wikiValues)
        {
            foreach ((GemGrade grade, string wikiValue) in wikiValues)
            {
                if (wikiValue.Contains('?'))
                {
                    continue;
                }

                if (!plannerValues.TryGetValue(grade, out string? plannerValue) ||
                    !string.Equals(
                        NormalizeGradeValueForComparison(plannerValue),
                        NormalizeGradeValueForComparison(wikiValue),
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static ModifierDraft ApplyPlannerOverride(ModifierDraft wikiDraft, ModifierDraft plannerDraft)
        {
            return wikiDraft with
            {
                VariantKey = plannerDraft.VariantKeyFor(wikiDraft.VocationRestriction),
                GradeValues = new Dictionary<GemGrade, string>(plannerDraft.GradeValues),
                Description = plannerDraft.Description ?? wikiDraft.Description
            };
        }

        private static bool MatchesPlannerCoverage(ModifierDraft existing, ModifierDraft planner)
        {
            if (!string.Equals(NormalizeModifierName(existing.Name), NormalizeModifierName(planner.Name), StringComparison.Ordinal) ||
                existing.Type != planner.Type)
            {
                return false;
            }

            return planner.VocationRestriction is null || existing.VocationRestriction == planner.VocationRestriction;
        }

        private static string CreateNameTypeKey(ModifierDraft draft)
        {
            return $"{draft.Type}:{NormalizeModifierName(draft.Name)}";
        }

        private static string NormalizeModifierName(string name)
        {
            return string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim().ToLowerInvariant();
        }

        private void ValidateMergedData(
            IReadOnlyCollection<GemDraft> gemDrafts,
            IReadOnlyCollection<ModifierDraft> modDrafts,
            WheelPlannerFullSnapshot plannerSnapshot,
            IReadOnlyCollection<ModDraft> wikiBasicMods,
            IReadOnlyCollection<ModDraft> wikiSupremeMods)
        {
            if (gemDrafts.Count < Math.Max(15, plannerSnapshot.Gems.Count))
            {
                throw new InvalidOperationException(
                    $"Gem import is incomplete. Expected at least {Math.Max(15, plannerSnapshot.Gems.Count)} gems but produced {gemDrafts.Count}.");
            }

            int basicCount = modDrafts.Count(draft => draft.Type == GemModifierType.Basic);
            int supremeCount = modDrafts.Count(draft => draft.Type == GemModifierType.Supreme);

            if (wikiBasicMods.Count > 0 && basicCount < wikiBasicMods.Count)
            {
                throw new InvalidOperationException(
                    $"Basic mod import is incomplete. TibiaWiki produced {wikiBasicMods.Count} rows but the merged catalog only contains {basicCount}.");
            }

            if (wikiSupremeMods.Count > 0 && supremeCount < wikiSupremeMods.Count)
            {
                throw new InvalidOperationException(
                    $"Supreme mod import is incomplete. TibiaWiki produced {wikiSupremeMods.Count} rows but the merged catalog only contains {supremeCount}.");
            }

            if (!modDrafts.Any(draft => draft.HasTradeoff))
            {
                throw new InvalidOperationException("Modifier import is incomplete. No tradeoff mods were detected.");
            }

            if (!modDrafts.Any(draft => draft.VocationRestriction is not null))
            {
                throw new InvalidOperationException("Modifier import is incomplete. No vocation-specific mods were detected.");
            }

            if (!modDrafts.Any(draft => draft.Name.Contains("Promotion Points", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Modifier import is incomplete. Revelation Mastery promotion point mods were not detected.");
            }
        }

        private async Task<GemImportResult> PersistGemsAsync(
            TibiaDbContext db,
            List<GemDraft> drafts,
            CancellationToken cancellationToken)
        {
            List<Gem> existing = await db.Gems.ToListAsync(cancellationToken);
            Dictionary<string, Gem> existingByName = existing.ToDictionary(gem => gem.Name, StringComparer.OrdinalIgnoreCase);
            HashSet<string> incomingNames = drafts.Select(draft => draft.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            DateTime now = DateTime.UtcNow;
            int added = 0;
            int updated = 0;
            int unchanged = 0;

            foreach (GemDraft draft in drafts)
            {
                if (!existingByName.TryGetValue(draft.Name, out Gem? tracked))
                {
                    db.Gems.Add(CreateGemEntity(draft, now));
                    added++;
                }
                else if (HasGemChanged(tracked, draft))
                {
                    UpdateGemEntity(tracked, draft, now);
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }

            int removed = existing.Count(entity => !incomingNames.Contains(entity.Name));
            if (removed > 0)
            {
                db.Gems.RemoveRange(existing.Where(entity => !incomingNames.Contains(entity.Name)));
            }

            return new GemImportResult(
                SourcePageCount: drafts.Count,
                GemsProcessed: drafts.Count,
                ModifiersProcessed: 0,
                Added: added,
                Updated: updated,
                Unchanged: unchanged,
                Removed: removed);
        }

        private async Task<ModImportResult> PersistModifiersAsync(
            TibiaDbContext db,
            List<ModifierDraft> drafts,
            CancellationToken cancellationToken)
        {
            List<GemModifier> existing = await db.GemModifiers
                .Include(modifier => modifier.Grades)
                .ToListAsync(cancellationToken);

            Dictionary<string, GemModifier> existingByKey = existing.ToDictionary(CreatePersistenceKey, StringComparer.OrdinalIgnoreCase);
            HashSet<string> incomingKeys = drafts.Select(CreatePersistenceKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

            DateTime now = DateTime.UtcNow;
            int added = 0;
            int updated = 0;
            int unchanged = 0;

            foreach (ModifierDraft draft in drafts)
            {
                string key = CreatePersistenceKey(draft);

                if (!existingByKey.TryGetValue(key, out GemModifier? tracked))
                {
                    db.GemModifiers.Add(CreateModifierEntity(draft, now));
                    added++;
                }
                else if (HasModifierChanged(tracked, draft))
                {
                    UpdateModifierEntity(tracked, draft, now, db);
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }

            int removed = existing.Count(entity => !incomingKeys.Contains(CreatePersistenceKey(entity)));
            if (removed > 0)
            {
                db.GemModifiers.RemoveRange(existing.Where(entity => !incomingKeys.Contains(CreatePersistenceKey(entity))));
            }

            int basicCount = drafts.Count(draft => draft.Type == GemModifierType.Basic);
            int supremeCount = drafts.Count(draft => draft.Type == GemModifierType.Supreme);

            return new ModImportResult(
                TotalProcessed: drafts.Count,
                BasicProcessed: basicCount,
                SupremeProcessed: supremeCount,
                Added: added,
                Updated: updated,
                Unchanged: unchanged,
                Removed: removed);
        }

        private static string CreatePersistenceKey(GemModifier entity)
        {
            return $"{entity.ModifierType}:{entity.VariantKey}";
        }

        private static string CreatePersistenceKey(ModifierDraft draft)
        {
            return $"{draft.Type}:{draft.VariantKey}";
        }

        private static Gem CreateGemEntity(GemDraft draft, DateTime now) => new()
        {
            Name = draft.Name,
            WikiUrl = draft.WikiUrl,
            GemFamily = draft.GemFamily,
            GemSize = draft.GemSize,
            VocationRestriction = draft.VocationRestriction,
            Description = draft.Description,
            LastUpdated = now
        };

        private static void UpdateGemEntity(Gem entity, GemDraft draft, DateTime now)
        {
            entity.WikiUrl = draft.WikiUrl;
            entity.GemFamily = draft.GemFamily;
            entity.GemSize = draft.GemSize;
            entity.VocationRestriction = draft.VocationRestriction;
            entity.Description = draft.Description;
            entity.LastUpdated = now;
        }

        private static bool HasGemChanged(Gem entity, GemDraft draft)
        {
            return entity.WikiUrl != draft.WikiUrl ||
                   entity.GemFamily != draft.GemFamily ||
                   entity.GemSize != draft.GemSize ||
                   entity.VocationRestriction != draft.VocationRestriction ||
                   entity.Description != draft.Description;
        }

        private static GemModifier CreateModifierEntity(ModifierDraft draft, DateTime now)
        {
            GemModifier modifier = new()
            {
                Name = draft.Name,
                VariantKey = draft.VariantKey,
                WikiUrl = $"https://tibia.fandom.com/wiki/{(draft.Type == GemModifierType.Basic ? "Basic_Mod" : "Supreme_Mod")}",
                ModifierType = draft.Type,
                Category = draft.Category,
                VocationRestriction = draft.VocationRestriction,
                IsComboMod = draft.IsCombo,
                HasTradeoff = draft.HasTradeoff,
                Description = draft.Description,
                LastUpdated = now
            };

            foreach ((GemGrade grade, string value) in draft.GradeValues.OrderBy(entry => entry.Key))
            {
                modifier.Grades.Add(new GemModifierGrade
                {
                    GemModifier = modifier,
                    Grade = grade,
                    ValueText = value,
                    ValueNumeric = TryParseNumeric(value),
                    IsIncomplete = value.Contains('?'),
                    LastUpdated = now
                });
            }

            return modifier;
        }

        private static void UpdateModifierEntity(GemModifier entity, ModifierDraft draft, DateTime now, TibiaDbContext db)
        {
            entity.Name = draft.Name;
            entity.VariantKey = draft.VariantKey;
            entity.WikiUrl = $"https://tibia.fandom.com/wiki/{(draft.Type == GemModifierType.Basic ? "Basic_Mod" : "Supreme_Mod")}";
            entity.Category = draft.Category;
            entity.VocationRestriction = draft.VocationRestriction;
            entity.IsComboMod = draft.IsCombo;
            entity.HasTradeoff = draft.HasTradeoff;
            entity.Description = draft.Description;
            entity.LastUpdated = now;

            foreach (GemModifierGrade existingGrade in entity.Grades.ToList())
            {
                if (!draft.GradeValues.ContainsKey(existingGrade.Grade))
                {
                    db.GemModifierGrades.Remove(existingGrade);
                }
            }

            foreach ((GemGrade grade, string value) in draft.GradeValues)
            {
                GemModifierGrade? existingGrade = entity.Grades.FirstOrDefault(entry => entry.Grade == grade);
                if (existingGrade is null)
                {
                    entity.Grades.Add(new GemModifierGrade
                    {
                        GemModifier = entity,
                        Grade = grade,
                        ValueText = value,
                        ValueNumeric = TryParseNumeric(value),
                        IsIncomplete = value.Contains('?'),
                        LastUpdated = now
                    });
                    continue;
                }

                existingGrade.ValueText = value;
                existingGrade.ValueNumeric = TryParseNumeric(value);
                existingGrade.IsIncomplete = value.Contains('?');
                existingGrade.LastUpdated = now;
            }
        }

        private static bool HasModifierChanged(GemModifier entity, ModifierDraft draft)
        {
            if (entity.Name != draft.Name ||
                entity.VariantKey != draft.VariantKey ||
                entity.Category != draft.Category ||
                entity.VocationRestriction != draft.VocationRestriction ||
                entity.IsComboMod != draft.IsCombo ||
                entity.HasTradeoff != draft.HasTradeoff ||
                entity.Description != draft.Description)
            {
                return true;
            }

            if (entity.Grades.Count != draft.GradeValues.Count)
            {
                return true;
            }

            foreach ((GemGrade grade, string value) in draft.GradeValues)
            {
                GemModifierGrade? existingGrade = entity.Grades.FirstOrDefault(entry => entry.Grade == grade);
                if (existingGrade is null || existingGrade.ValueText != value)
                {
                    return true;
                }
            }

            return false;
        }

        private static decimal? TryParseNumeric(string value)
        {
            string cleaned = value.Trim().TrimStart('+').TrimEnd('%');
            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result)
                ? result
                : null;
        }

        private static string NormalizeGradeValueForComparison(string value)
        {
            string[] segments = value
                .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => NormalizeGradeValueSegment(segment))
                .ToArray();

            return string.Join("/", segments);
        }

        private static string NormalizeGradeValueSegment(string segment)
        {
            string trimmed = segment.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "?")
            {
                return trimmed;
            }

            if (char.IsDigit(trimmed[0]))
            {
                trimmed = $"+{trimmed}";
            }

            string sign = trimmed[0] is '+' or '-' ? trimmed[..1] : string.Empty;
            string remainder = sign.Length > 0 ? trimmed[1..] : trimmed;

            int suffixIndex = 0;
            while (suffixIndex < remainder.Length &&
                   (char.IsDigit(remainder[suffixIndex]) || remainder[suffixIndex] == '.'))
            {
                suffixIndex++;
            }

            string numericPart = remainder[..suffixIndex];
            string suffix = remainder[suffixIndex..].Replace(" ", string.Empty, StringComparison.Ordinal);

            if (!decimal.TryParse(numericPart, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal numericValue))
            {
                return $"{sign}{remainder.Replace(" ", string.Empty, StringComparison.Ordinal)}";
            }

            return $"{sign}{numericValue.ToString("0.##", CultureInfo.InvariantCulture)}{suffix}";
        }

        private static bool DetectComboMod(string name)
        {
            return name.Contains('/');
        }

        private static bool DetectTradeoffMod(IReadOnlyDictionary<GemGrade, string> gradeValues)
        {
            return gradeValues.Values.Any(value => value.Contains(" / -", StringComparison.Ordinal));
        }

        private sealed record GemDraft(
            string Name,
            string WikiUrl,
            GemFamily GemFamily,
            GemSize GemSize,
            GemVocation? VocationRestriction,
            string? Description);

        private sealed record ModifierDraft(
            string VariantKey,
            string Name,
            GemModifierType Type,
            GemModifierCategory Category,
            GemVocation? VocationRestriction,
            Dictionary<GemGrade, string> GradeValues,
            bool IsCombo,
            bool HasTradeoff,
            string? Description)
        {
            public string VariantKeyFor(GemVocation? vocationRestriction)
            {
                if (VocationRestriction is null && vocationRestriction is not null && VariantKey.StartsWith("official-supreme-", StringComparison.Ordinal))
                {
                    return $"{VariantKey}-{vocationRestriction.Value.ToString().ToLowerInvariant()}";
                }

                return VariantKey;
            }
        }
    }

    public sealed record ModImportResult(
        int TotalProcessed,
        int BasicProcessed,
        int SupremeProcessed,
        int Added,
        int Updated,
        int Unchanged,
        int Removed);
}
