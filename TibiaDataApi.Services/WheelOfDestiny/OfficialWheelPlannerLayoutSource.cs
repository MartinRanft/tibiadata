using System.Text.Json;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PuppeteerSharp;

using TibiaDataApi.Services.Entities.WheelOfDestiny;
using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.WheelOfDestiny
{
    public sealed class OfficialWheelPlannerLayoutSource(
        IHostEnvironment hostEnvironment,
        ILogger<OfficialWheelPlannerLayoutSource> logger) : IWheelPlannerLayoutSource
    {
        private const string PlannerUrl = "https://www.tibia.com/community/?subtopic=wheelofdestinyplanner";
        private static readonly TimeSpan PlannerSelectionDelay = TimeSpan.FromMilliseconds(250);

        private static readonly PlannerTileDefinition[] TileDefinitions =
        [
            new("QTL0", WheelQuarter.TopLeft, 0, 1, 180, 270),
            new("QTL1", WheelQuarter.TopLeft, 1, 2, 180, 225),
            new("QTL3", WheelQuarter.TopLeft, 1, 3, 225, 270),
            new("QTL2", WheelQuarter.TopLeft, 2, 4, 180, 210),
            new("QTL4", WheelQuarter.TopLeft, 2, 5, 210, 240),
            new("QTL6", WheelQuarter.TopLeft, 2, 6, 240, 270),
            new("QTL5", WheelQuarter.TopLeft, 3, 7, 195, 225),
            new("QTL7", WheelQuarter.TopLeft, 3, 8, 225, 255),
            new("QTL8", WheelQuarter.TopLeft, 4, 9, 195, 255),
            new("QTR0", WheelQuarter.TopRight, 0, 1, 270, 360),
            new("QTR3", WheelQuarter.TopRight, 1, 2, 270, 315),
            new("QTR1", WheelQuarter.TopRight, 1, 3, 315, 360),
            new("QTR6", WheelQuarter.TopRight, 2, 4, 270, 300),
            new("QTR4", WheelQuarter.TopRight, 2, 5, 300, 330),
            new("QTR2", WheelQuarter.TopRight, 2, 6, 330, 360),
            new("QTR7", WheelQuarter.TopRight, 3, 7, 285, 315),
            new("QTR5", WheelQuarter.TopRight, 3, 8, 315, 345),
            new("QTR8", WheelQuarter.TopRight, 4, 9, 285, 345),
            new("QBL0", WheelQuarter.BottomLeft, 0, 1, 90, 180),
            new("QBL3", WheelQuarter.BottomLeft, 1, 2, 90, 135),
            new("QBL1", WheelQuarter.BottomLeft, 1, 3, 135, 180),
            new("QBL6", WheelQuarter.BottomLeft, 2, 4, 90, 120),
            new("QBL4", WheelQuarter.BottomLeft, 2, 5, 120, 150),
            new("QBL2", WheelQuarter.BottomLeft, 2, 6, 150, 180),
            new("QBL7", WheelQuarter.BottomLeft, 3, 7, 105, 135),
            new("QBL5", WheelQuarter.BottomLeft, 3, 8, 135, 165),
            new("QBL8", WheelQuarter.BottomLeft, 4, 9, 105, 165),
            new("QBR0", WheelQuarter.BottomRight, 0, 1, 0, 90),
            new("QBR1", WheelQuarter.BottomRight, 1, 2, 0, 45),
            new("QBR3", WheelQuarter.BottomRight, 1, 3, 45, 90),
            new("QBR2", WheelQuarter.BottomRight, 2, 4, 0, 30),
            new("QBR4", WheelQuarter.BottomRight, 2, 5, 30, 60),
            new("QBR6", WheelQuarter.BottomRight, 2, 6, 60, 90),
            new("QBR5", WheelQuarter.BottomRight, 3, 7, 15, 45),
            new("QBR7", WheelQuarter.BottomRight, 3, 8, 45, 75),
            new("QBR8", WheelQuarter.BottomRight, 4, 9, 15, 75)
        ];

        private static readonly PlannerCornerDefinition[] CornerDefinitions =
        [
            new("QTL", WheelQuarter.TopLeft, 225),
            new("QTR", WheelQuarter.TopRight, 315),
            new("QBL", WheelQuarter.BottomLeft, 135),
            new("QBR", WheelQuarter.BottomRight, 45)
        ];

        private static readonly PlannerVocationDefinition[] VocationDefinitions =
        [
            new("knight", WheelVocation.EliteKnight),
            new("paladin", WheelVocation.RoyalPaladin),
            new("druid", WheelVocation.ElderDruid),
            new("sorcerer", WheelVocation.MasterSorcerer),
            new("monk", WheelVocation.ExaltedMonk)
        ];

        private static readonly PlannerGemVocationDefinition[] GemVocationDefinitions =
        [
            new("knight", GemVocation.Knight),
            new("paladin", GemVocation.Paladin),
            new("druid", GemVocation.Druid),
            new("sorcerer", GemVocation.Sorcerer),
            new("monk", GemVocation.Monk)
        ];

        public async Task<WheelPlannerLayoutSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            string browserExecutablePath = ResolveBrowserExecutablePath();

            await using IBrowser browser = await LaunchBrowserAsync(browserExecutablePath);
            await using IPage page = await CreateInitializedPageAsync(browser);

            return await ScrapeLayoutAsync(page, cancellationToken);
        }

        public async Task<WheelPlannerFullSnapshot> LoadFullAsync(CancellationToken cancellationToken = default)
        {
            string browserExecutablePath = ResolveBrowserExecutablePath();

            await using IBrowser browser = await LaunchBrowserAsync(browserExecutablePath);
            await using IPage page = await CreateInitializedPageAsync(browser);

            WheelPlannerLayoutSnapshot layout = await ScrapeLayoutAsync(page, cancellationToken);

            string libraryJson = await LoadSkillwheelLibraryJsonAsync(page);

            using JsonDocument document = JsonDocument.Parse(libraryJson);
            JsonElement root = document.RootElement;

            List<WheelPlannerGemSnapshot> gems = ParseGems(root);
            List<WheelPlannerModSnapshot> mods = ParseMods(root);

            logger.LogInformation(
                "Loaded full wheel planner snapshot with {GemCount} gems and {ModCount} mods.",
                gems.Count,
                mods.Count);

            WheelPlannerFullSnapshot snapshot = new(layout, gems, mods);

            await SaveSnapshotAsync(snapshot, cancellationToken);

            return snapshot;
        }

        private async Task SaveSnapshotAsync(WheelPlannerFullSnapshot snapshot, CancellationToken cancellationToken)
        {
            try
            {
                string dir = ResolveSeedSavePath();
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, EmbeddedWheelPlannerLayoutSource.SnapshotFileName);

                string json = JsonSerializer.Serialize(snapshot, EmbeddedWheelPlannerLayoutSource.JsonOptions);
                await File.WriteAllTextAsync(path, json, cancellationToken);

                logger.LogInformation("Wheel planner snapshot saved to '{Path}'.", path);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to save wheel planner snapshot to seed file.");
            }
        }

        private string ResolveSeedSavePath()
        {
            string? overridePath = Environment.GetEnvironmentVariable("WHEEL_SEED_SAVE_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                return overridePath;
            }

            
            string? solutionRoot = Path.GetDirectoryName(hostEnvironment.ContentRootPath.TrimEnd(Path.DirectorySeparatorChar));
            string candidate = Path.Combine(
                solutionRoot ?? hostEnvironment.ContentRootPath,
                "TibiaDataApi.Services",
                "WheelOfDestiny",
                "Seed");

            return candidate;
        }

        private async Task<WheelPlannerLayoutSnapshot> ScrapeLayoutAsync(IPage page, CancellationToken cancellationToken)
        {
            List<WheelPlannerSectionSnapshot> sections = [];
            List<WheelPlannerRevelationSlotSnapshot> revelationSlots = [];

            foreach (PlannerVocationDefinition vocation in VocationDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await page.ClickAsync($"#wod-vocation_{vocation.PlannerSlug}");
                await Task.Delay(PlannerSelectionDelay, cancellationToken);

                foreach (PlannerTileDefinition tile in TileDefinitions)
                {
                    PlannerSectionHoverPayload payload = await page.EvaluateFunctionAsync<PlannerSectionHoverPayload>(
                        """
                        (tile) => {
                            const canvas = document.querySelector('#wod-canvas');
                            const rect = canvas.getBoundingClientRect();
                            const pivot = 261;
                            const tileSize = 50;
                            const border = 2;
                            const outerRadius = index => index * (tileSize + border) + 1;
                            const innerRadius = index => outerRadius(index) + tileSize;
                            const angle = ((tile.startAngleDegrees + tile.endAngleDegrees) / 2) * Math.PI / 180;
                            const radius = (outerRadius(tile.radiusIndex) + innerRadius(tile.radiusIndex)) / 2;
                            const clientX = rect.left + pivot + Math.cos(angle) * radius;
                            const clientY = rect.top + pivot + Math.sin(angle) * radius;
                            canvas.dispatchEvent(new MouseEvent('mousemove', { bubbles: true, cancelable: true, clientX, clientY }));
                            return {
                                bar: document.querySelector('#wod-information-box-bar-text')?.textContent?.trim() ?? null,
                                dedication: document.querySelector('#wod-information-box-dedication-value')?.innerText?.trim() ?? null,
                                conviction: document.querySelector('#wod-information-box-conviction-value')?.innerText?.trim() ?? null
                            };
                        }
                        """,
                        tile);

                    sections.Add(new WheelPlannerSectionSnapshot(
                        vocation.Vocation,
                        tile.Id,
                        tile.Quarter,
                        tile.RadiusIndex,
                        tile.SortOrder,
                        ParseRequiredPoints(payload.Bar, tile.Id),
                        payload.Dedication ?? throw new InvalidOperationException($"Planner dedication text missing for section '{tile.Id}'."),
                        payload.Conviction ?? throw new InvalidOperationException($"Planner conviction text missing for section '{tile.Id}'.")));
                }

                foreach (PlannerCornerDefinition corner in CornerDefinitions)
                {
                    PlannerRevelationHoverPayload payload = await page.EvaluateFunctionAsync<PlannerRevelationHoverPayload>(
                        """
                        (corner) => {
                            const canvas = document.querySelector('#wod-canvas');
                            const rect = canvas.getBoundingClientRect();
                            const pivot = 261;
                            const radius = 311;
                            const angle = corner.angleDegrees * Math.PI / 180;
                            const clientX = rect.left + pivot + Math.cos(angle) * radius;
                            const clientY = rect.top + pivot + Math.sin(angle) * radius;
                            canvas.dispatchEvent(new MouseEvent('mousemove', { bubbles: true, cancelable: true, clientX, clientY }));
                            return {
                                bar: document.querySelector('#wod-information-box-bar-text')?.textContent?.trim() ?? null,
                                name: document.querySelector('#wod-information-box-revelation-name')?.innerText?.trim() ?? null
                            };
                        }
                        """,
                        corner);

                    revelationSlots.Add(new WheelPlannerRevelationSlotSnapshot(
                        vocation.Vocation,
                        corner.SlotKey,
                        corner.Quarter,
                        ParseRequiredPoints(payload.Bar, corner.SlotKey),
                        payload.Name ?? throw new InvalidOperationException($"Planner revelation name missing for slot '{corner.SlotKey}'.")));
                }
            }

            logger.LogInformation(
                "Loaded official wheel planner layout with {SectionCount} sections and {SlotCount} revelation slots.",
                sections.Count,
                revelationSlots.Count);

            return new WheelPlannerLayoutSnapshot(sections, revelationSlots);
        }

        private async Task<string> LoadSkillwheelLibraryJsonAsync(IPage page)
        {
            string? libraryJson = await page.EvaluateFunctionAsync<string?>(
                """
                async () => {
                    const plannerScript = Array.from(document.scripts)
                        .find(script => (script.textContent ?? '').includes('SkillwheelStringsJsonLibrary.json'));
                    const match = plannerScript?.textContent?.match(/runWodPlanner\("([^"]+)"/);
                    if (!match) {
                        return null;
                    }

                    const response = await fetch(match[1]);
                    return await response.text();
                }
                """);

            if (string.IsNullOrWhiteSpace(libraryJson))
            {
                throw new InvalidOperationException("Could not load the official skillwheel string library from tibia.com.");
            }

            return libraryJson;
        }

        private static List<WheelPlannerGemSnapshot> ParseGems(JsonElement root)
        {
            if (!root.TryGetProperty("VesselInfos", out JsonElement vesselInfos) ||
                !vesselInfos.TryGetProperty("GemNames", out JsonElement gemNames))
            {
                return [];
            }

            string? description = vesselInfos.TryGetProperty("GemInfo", out JsonElement gemInfoElement)
                ? gemInfoElement.GetString()
                : null;

            List<WheelPlannerGemSnapshot> gems = [];

            foreach (JsonProperty sizeProperty in gemNames.EnumerateObject())
            {
                GemSize size = sizeProperty.Name switch
                {
                    "0" => GemSize.Lesser,
                    "1" => GemSize.Regular,
                    "2" => GemSize.Greater,
                    _ => throw new InvalidOperationException($"Unknown official gem size key '{sizeProperty.Name}'.")
                };

                foreach (JsonProperty vocationProperty in sizeProperty.Value.EnumerateObject())
                {
                    string? name = vocationProperty.Value.GetString();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    gems.Add(new WheelPlannerGemSnapshot(
                        name,
                        ParseGemFamily(name),
                        size,
                        MapGemVocation(vocationProperty.Name),
                        []));
                }
            }

            return gems
                   .OrderBy(entry => entry.Name, StringComparer.Ordinal)
                   .ToList();
        }

        private static List<WheelPlannerModSnapshot> ParseMods(JsonElement root)
        {
            List<WheelPlannerModSnapshot> mods = [];
            mods.AddRange(ParseBasicMods(root));
            mods.AddRange(ParseSupremeMods(root));
            return mods;
        }

        private static IEnumerable<WheelPlannerModSnapshot> ParseBasicMods(JsonElement root)
        {
            if (!root.TryGetProperty("BasicModConfig", out JsonElement basicConfig) ||
                !root.TryGetProperty("BasicModEffectInfos", out JsonElement basicEffectInfos))
            {
                return [];
            }

            List<WheelPlannerModSnapshot> mods = [];

            foreach (JsonProperty configProperty in basicConfig.EnumerateObject())
            {
                List<BasicEffectEntry> effects = [];

                foreach (JsonElement effectElement in configProperty.Value.EnumerateArray())
                {
                    string effectId = effectElement.GetProperty("EffectId").GetString()
                        ?? throw new InvalidOperationException($"Basic mod '{configProperty.Name}' is missing an EffectId.");

                    JsonElement effectInfo = basicEffectInfos.GetProperty(effectId);
                    string effectName = GetPrimaryAlias(effectInfo.GetProperty("Name").GetString());

                    Dictionary<GemVocation, IReadOnlyDictionary<GemGrade, string>> valuesByVocation = new();
                    foreach (PlannerGemVocationDefinition vocation in GemVocationDefinitions)
                    {
                        JsonElement vocationValues = effectElement.GetProperty(vocation.Slug);
                        valuesByVocation[vocation.Vocation] = BuildGradeValues(vocationValues);
                    }

                    effects.Add(new BasicEffectEntry(effectName, valuesByVocation));
                }

                if (effects.Count == 0)
                {
                    continue;
                }

                string displayName = string.Join(" / ", effects.Select(entry => entry.Name));
                bool isGeneral = AllBasicVocationValuesEqual(effects);

                if (isGeneral)
                {
                    mods.Add(new WheelPlannerModSnapshot(
                        VariantKey: $"official-basic-{configProperty.Name}",
                        Name: displayName,
                        Type: GemModifierType.Basic,
                        Category: GemModifierCategory.General,
                        VocationRestriction: null,
                        GradeValues: BuildCombinedBasicGradeValues(effects, GemVocation.Knight),
                        Description: null));

                    continue;
                }

                foreach (PlannerGemVocationDefinition vocation in GemVocationDefinitions)
                {
                    mods.Add(new WheelPlannerModSnapshot(
                        VariantKey: $"official-basic-{configProperty.Name}-{vocation.Slug}",
                        Name: displayName,
                        Type: GemModifierType.Basic,
                        Category: GemModifierCategory.VocationSpecific,
                        VocationRestriction: vocation.Vocation,
                        GradeValues: BuildCombinedBasicGradeValues(effects, vocation.Vocation),
                        Description: null));
                }
            }

            return mods;
        }

        private static IEnumerable<WheelPlannerModSnapshot> ParseSupremeMods(JsonElement root)
        {
            if (!root.TryGetProperty("SupremeModInfos", out JsonElement supremeInfos))
            {
                return [];
            }

            List<WheelPlannerModSnapshot> mods = [];

            foreach (JsonProperty infoProperty in supremeInfos.EnumerateObject())
            {
                string name = BuildSupremeName(infoProperty.Value);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                mods.Add(new WheelPlannerModSnapshot(
                    VariantKey: $"official-supreme-{infoProperty.Name}",
                    Name: name,
                    Type: GemModifierType.Supreme,
                    Category: string.IsNullOrWhiteSpace(GetPrimaryAlias(infoProperty.Value.GetProperty("Name").GetString()))
                        ? GemModifierCategory.General
                        : GemModifierCategory.VocationSpecific,
                    VocationRestriction: null,
                    GradeValues: BuildGradeValues(infoProperty.Value.GetProperty("EffectInfo")),
                    Description: BuildSupremeDescription(infoProperty.Value)));
            }

            return mods;
        }

        private static bool AllBasicVocationValuesEqual(IReadOnlyList<BasicEffectEntry> effects)
        {
            IReadOnlyDictionary<GemGrade, string>? baseline = null;

            foreach (PlannerGemVocationDefinition vocation in GemVocationDefinitions)
            {
                IReadOnlyDictionary<GemGrade, string> current = BuildCombinedBasicGradeValues(effects, vocation.Vocation);
                if (baseline is null)
                {
                    baseline = current;
                    continue;
                }

                if (!baseline.OrderBy(entry => entry.Key).SequenceEqual(current.OrderBy(entry => entry.Key)))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<GemGrade, string> BuildCombinedBasicGradeValues(
            IReadOnlyList<BasicEffectEntry> effects,
            GemVocation vocation)
        {
            Dictionary<GemGrade, string> combined = new();

            foreach (GemGrade grade in Enum.GetValues<GemGrade>())
            {
                combined[grade] = string.Join(
                    " / ",
                    effects.Select(effect => effect.ValuesByVocation[vocation][grade]));
            }

            return combined;
        }

        private static Dictionary<GemGrade, string> BuildGradeValues(JsonElement gradeValuesElement)
        {
            return new Dictionary<GemGrade, string>
            {
                [GemGrade.GradeI] = gradeValuesElement.GetProperty("0").GetString() ?? string.Empty,
                [GemGrade.GradeII] = gradeValuesElement.GetProperty("1").GetString() ?? string.Empty,
                [GemGrade.GradeIII] = gradeValuesElement.GetProperty("2").GetString() ?? string.Empty,
                [GemGrade.GradeIV] = gradeValuesElement.GetProperty("3").GetString() ?? string.Empty
            };
        }

        private static GemFamily ParseGemFamily(string name)
        {
            return name switch
            {
                var value when value.Contains("Guardian", StringComparison.OrdinalIgnoreCase) => GemFamily.Guardian,
                var value when value.Contains("Marksman", StringComparison.OrdinalIgnoreCase) => GemFamily.Marksman,
                var value when value.Contains("Mystic", StringComparison.OrdinalIgnoreCase) => GemFamily.Mystic,
                var value when value.Contains("Sage", StringComparison.OrdinalIgnoreCase) => GemFamily.Sage,
                var value when value.Contains("Spiritualist", StringComparison.OrdinalIgnoreCase) => GemFamily.Spiritualist,
                _ => throw new InvalidOperationException($"Unknown official gem family in '{name}'.")
            };
        }

        private static GemVocation MapGemVocation(string plannerSlug)
        {
            return plannerSlug switch
            {
                "knight" => GemVocation.Knight,
                "paladin" => GemVocation.Paladin,
                "druid" => GemVocation.Druid,
                "sorcerer" => GemVocation.Sorcerer,
                "monk" => GemVocation.Monk,
                _ => throw new InvalidOperationException($"Unknown official gem vocation '{plannerSlug}'.")
            };
        }

        private static string BuildSupremeName(JsonElement info)
        {
            string primaryName = GetPrimaryAlias(info.GetProperty("Name").GetString());
            string primaryNameSummary = GetPrimaryAlias(info.GetProperty("NameSummary").GetString());

            if (string.IsNullOrWhiteSpace(primaryName))
            {
                return primaryNameSummary;
            }

            if (string.Equals(primaryName, "Revelation Mastery", StringComparison.Ordinal))
            {
                string targetName = primaryNameSummary.StartsWith("Revelation Mastery ", StringComparison.Ordinal)
                    ? primaryNameSummary["Revelation Mastery ".Length..]
                    : primaryNameSummary;
                return $"Revelation Mastery {targetName} - Promotion Points";
            }

            string baseName = primaryName.StartsWith("Augmented ", StringComparison.Ordinal)
                ? primaryName["Augmented ".Length..]
                : primaryName;
            string effectLabel = BuildSupremeEffectLabel(info);

            return string.IsNullOrWhiteSpace(effectLabel)
                ? baseName
                : $"{baseName} - {effectLabel}";
        }

        private static string BuildSupremeEffectLabel(JsonElement info)
        {
            string[] gradeTexts = info.GetProperty("EffectInfo")
                                      .EnumerateObject()
                                      .OrderBy(entry => entry.Name, StringComparer.Ordinal)
                                      .Select(entry => entry.Value.GetString() ?? string.Empty)
                                      .ToArray();

            if (gradeTexts.Any(text => text.Contains("Cooldown", StringComparison.Ordinal)))
            {
                return gradeTexts.Any(text => text.Contains("Momentum", StringComparison.Ordinal))
                    ? "Cooldown / Momentum"
                    : "Cooldown";
            }

            if (gradeTexts.Any(text => text.Contains("Critical Extra Damage", StringComparison.Ordinal)))
            {
                return "Critical Extra Damage";
            }

            if (gradeTexts.Any(text => text.Contains("Base Damage", StringComparison.Ordinal)))
            {
                return "Base Damage";
            }

            if (gradeTexts.Any(text => text.Contains("Base Healing", StringComparison.Ordinal)))
            {
                return "Base Healing";
            }

            return string.Empty;
        }

        private static string? BuildSupremeDescription(JsonElement info)
        {
            string? summary = info.TryGetProperty("EffectInfoSummary", out JsonElement summaryElement)
                ? summaryElement.GetString()
                : null;

            return string.IsNullOrWhiteSpace(summary) || summary.Contains("<ReplaceMe>", StringComparison.Ordinal)
                ? null
                : summary;
        }

        private static string GetPrimaryAlias(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            string primary = rawValue.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
            return primary.Trim();
        }

        private static short ParseRequiredPoints(string? rawBarText, string entryKey)
        {
            if (string.IsNullOrWhiteSpace(rawBarText))
            {
                throw new InvalidOperationException($"Planner progress text missing for '{entryKey}'.");
            }

            string[] parts = rawBarText.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !short.TryParse(parts[1], out short points))
            {
                throw new InvalidOperationException($"Planner progress text '{rawBarText}' for '{entryKey}' is invalid.");
            }

            return points;
        }

        private static async Task<IBrowser> LaunchBrowserAsync(string browserExecutablePath)
        {
            return await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = browserExecutablePath,
                Args =
                [
                    "--no-sandbox",
                    "--disable-gpu",
                    "--disable-dev-shm-usage"
                ]
            });
        }

        private static async Task<IPage> CreateInitializedPageAsync(IBrowser browser)
        {
            IPage page = await browser.NewPageAsync();

            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1400,
                Height = 1800
            });

            await page.GoToAsync(
                PlannerUrl,
                new NavigationOptions
                {
                    WaitUntil = [WaitUntilNavigation.Networkidle2],
                    Timeout = 120_000
                });

            await page.WaitForSelectorAsync(
                "#wod-vocation_knight",
                new WaitForSelectorOptions
                {
                    Timeout = 120_000
                });

            return page;
        }

        private static string ResolveBrowserExecutablePath()
        {
            string? configuredPath = Environment.GetEnvironmentVariable("WHEEL_PLANNER_BROWSER_EXECUTABLE_PATH");
            if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            {
                return configuredPath;
            }

            string[] candidates =
            [
                "/usr/bin/google-chrome-stable",
                "/usr/bin/google-chrome",
                "/usr/bin/chromium",
                "/usr/bin/chromium-browser"
            ];

            string? match = candidates.FirstOrDefault(File.Exists);
            return match ?? throw new FileNotFoundException(
                "No supported browser executable was found for the wheel planner import. Set WHEEL_PLANNER_BROWSER_EXECUTABLE_PATH or install Google Chrome / Chromium.");
        }

        private sealed record PlannerVocationDefinition(
            string PlannerSlug,
            WheelVocation Vocation);

        private sealed record PlannerGemVocationDefinition(
            string Slug,
            GemVocation Vocation);

        private sealed record PlannerTileDefinition(
            string Id,
            WheelQuarter Quarter,
            byte RadiusIndex,
            short SortOrder,
            int StartAngleDegrees,
            int EndAngleDegrees);

        private sealed record PlannerCornerDefinition(
            string SlotKey,
            WheelQuarter Quarter,
            int AngleDegrees);

        private sealed record BasicEffectEntry(
            string Name,
            IReadOnlyDictionary<GemVocation, IReadOnlyDictionary<GemGrade, string>> ValuesByVocation);

        private sealed class PlannerSectionHoverPayload
        {
            public string? Bar { get; init; }

            public string? Dedication { get; init; }

            public string? Conviction { get; init; }
        }

        private sealed class PlannerRevelationHoverPayload
        {
            public string? Bar { get; init; }

            public string? Name { get; init; }
        }
    }
}
