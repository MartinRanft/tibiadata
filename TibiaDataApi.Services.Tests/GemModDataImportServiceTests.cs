using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.Entities.WheelOfDestiny;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper.Implementations;
using TibiaDataApi.Services.TibiaWiki;
using TibiaDataApi.Services.WheelOfDestiny;
using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.Tests
{
    public sealed class GemModDataImportServiceTests
    {
        [Fact]
        public async Task ImportGemsAsync_CreatesGemCatalogWithVocationRestrictions()
        {
            await using TibiaDbContext db = CreateDbContext();
            HybridGemModDataImportService service = CreateService();

            GemImportResult result = await service.ImportGemsAsync(db);

            Assert.Equal(15, result.GemsProcessed);

            List<Gem> gems = await db.Gems.OrderBy(gem => gem.Name).ToListAsync();
            Assert.Equal(15, gems.Count);
            Assert.Contains(gems, gem => gem.Name == "Guardian Gem" && gem.VocationRestriction == GemVocation.Knight);
            Assert.Contains(gems, gem => gem.Name == "Spiritualist Gem" && gem.VocationRestriction == GemVocation.Monk);
        }

        [Fact]
        public async Task ImportGemsAsync_PersistsDuplicateGeneralBasicModNamesAsDistinctVariants()
        {
            await using TibiaDbContext db = CreateDbContext();
            HybridGemModDataImportService service = CreateService();

            await service.ImportGemsAsync(db);

            List<GemModifier> mods = await db.GemModifiers
                .Include(modifier => modifier.Grades)
                .Where(modifier => modifier.Name == "Fire Resistance / Earth Resistance" && modifier.ModifierType == GemModifierType.Basic)
                .OrderBy(modifier => modifier.VariantKey)
                .ToListAsync();

            Assert.Equal(2, mods.Count);
            Assert.Equal(
                ["+1% / +1%", "+3% / -2%"],
                mods.Select(modifier => modifier.Grades.Single(grade => grade.Grade == GemGrade.GradeI).ValueText)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray());
        }

        [Fact]
        public async Task ImportGemsAsync_UsesOfficialValuesForUnknownAndSharedSupremeRows()
        {
            await using TibiaDbContext db = CreateDbContext();
            HybridGemModDataImportService service = CreateService();

            await service.ImportGemsAsync(db);

            GemModifier monkHitPoints = await db.GemModifiers
                .Include(modifier => modifier.Grades)
                .SingleAsync(modifier =>
                    modifier.Name == "Hit Points" &&
                    modifier.ModifierType == GemModifierType.Basic &&
                    modifier.VocationRestriction == GemVocation.Monk);

            Assert.Equal("+200", monkHitPoints.Grades.Single(grade => grade.Grade == GemGrade.GradeI).ValueText);
            Assert.DoesNotContain(monkHitPoints.Grades, grade => grade.IsIncomplete);

            List<GemModifier> ultimateHealing = await db.GemModifiers
                .Include(modifier => modifier.Grades)
                .Where(modifier =>
                    modifier.Name == "Ultimate Healing - Base Healing" &&
                    modifier.ModifierType == GemModifierType.Supreme)
                .OrderBy(modifier => modifier.VocationRestriction)
                .ToListAsync();

            Assert.Equal(2, ultimateHealing.Count);
            Assert.Contains(ultimateHealing, modifier => modifier.VocationRestriction == GemVocation.Druid);
            Assert.Contains(ultimateHealing, modifier => modifier.VocationRestriction == GemVocation.Sorcerer);
            Assert.All(ultimateHealing, modifier =>
                Assert.Equal("+5%", modifier.Grades.Single(grade => grade.Grade == GemGrade.GradeI).ValueText));

            Assert.DoesNotContain(
                await db.GemModifiers.Select(modifier => modifier.Name).ToListAsync(),
                name => string.Equals(name, "[[]]", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ImportGemsAsync_ReusesExistingTransaction()
        {
            await using TibiaDbContext db = CreateDbContext();
            HybridGemModDataImportService service = CreateService();
            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

            GemImportResult result = await service.ImportGemsAsync(db);

            Assert.Equal(15, result.GemsProcessed);
            Assert.True(await db.Gems.AnyAsync(gem => gem.Name == "Guardian Gem"));

            await transaction.CommitAsync();
        }

        private static HybridGemModDataImportService CreateService()
        {
            FakeTibiaWikiHttpService tibiaWikiHttpService = new();
            BasicModTableScraper basicModScraper = new(tibiaWikiHttpService, NullLogger<BasicModTableScraper>.Instance);
            SupremeModTableScraper supremeModScraper = new(tibiaWikiHttpService, NullLogger<SupremeModTableScraper>.Instance);

            return new HybridGemModDataImportService(
                new StubWheelPlannerLayoutSource(),
                basicModScraper,
                supremeModScraper,
                NullLogger<HybridGemModDataImportService>.Instance);
        }

        private sealed class StubWheelPlannerLayoutSource : IWheelPlannerLayoutSource
        {
            public Task<WheelPlannerLayoutSnapshot> LoadAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(WheelPlannerLayoutSnapshot.Empty);
            }

            public Task<WheelPlannerFullSnapshot> LoadFullAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new WheelPlannerFullSnapshot(
                    WheelPlannerLayoutSnapshot.Empty,
                    [],
                    [
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-basic-9",
                            Name: "Fire Resistance / Earth Resistance",
                            Type: GemModifierType.Basic,
                            Category: GemModifierCategory.General,
                            VocationRestriction: null,
                            GradeValues: CreateGrades("+1% / +1%", "+1.1% / +1.1%", "+1.2% / +1.2%", "+1.5% / +1.5%"),
                            Description: null),
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-basic-15",
                            Name: "Fire Resistance / Earth Resistance",
                            Type: GemModifierType.Basic,
                            Category: GemModifierCategory.General,
                            VocationRestriction: null,
                            GradeValues: CreateGrades("+3% / -2%", "+3.3% / -2%", "+3.6% / -2%", "+4.5% / -2%"),
                            Description: null),
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-basic-7",
                            Name: "Holy Resistance / Death Resistance",
                            Type: GemModifierType.Basic,
                            Category: GemModifierCategory.General,
                            VocationRestriction: null,
                            GradeValues: CreateGrades("+1.5% / -1%", "+1.65% / -1%", "+1.8% / -1%", "+2.25% / -1%"),
                            Description: null),
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-basic-31-monk",
                            Name: "Hit Points",
                            Type: GemModifierType.Basic,
                            Category: GemModifierCategory.VocationSpecific,
                            VocationRestriction: GemVocation.Monk,
                            GradeValues: CreateGrades("+200", "+220", "+240", "+300"),
                            Description: null),
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-basic-33-druid",
                            Name: "Mana / Fire Resistance",
                            Type: GemModifierType.Basic,
                            Category: GemModifierCategory.VocationSpecific,
                            VocationRestriction: GemVocation.Druid,
                            GradeValues: CreateGrades("+300 / +1%", "+330 / +1.1%", "+360 / +1.2%", "+450 / +1.5%"),
                            Description: null),
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-basic-33-sorcerer",
                            Name: "Mana / Fire Resistance",
                            Type: GemModifierType.Basic,
                            Category: GemModifierCategory.VocationSpecific,
                            VocationRestriction: GemVocation.Sorcerer,
                            GradeValues: CreateGrades("+300 / +1%", "+330 / +1.1%", "+360 / +1.2%", "+450 / +1.5%"),
                            Description: null),
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-supreme-0",
                            Name: "Dodge",
                            Type: GemModifierType.Supreme,
                            Category: GemModifierCategory.General,
                            VocationRestriction: null,
                            GradeValues: CreateGrades("+0.28%", "+0.31%", "+0.34%", "+0.42%"),
                            Description: null),
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-supreme-4",
                            Name: "Ultimate Healing - Base Healing",
                            Type: GemModifierType.Supreme,
                            Category: GemModifierCategory.VocationSpecific,
                            VocationRestriction: null,
                            GradeValues: CreateGrades("+5%", "+5.5%", "+6%", "+7.5%"),
                            Description: null),
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-supreme-5",
                            Name: "Revelation Mastery Gift of Life - Promotion Points",
                            Type: GemModifierType.Supreme,
                            Category: GemModifierCategory.VocationSpecific,
                            VocationRestriction: null,
                            GradeValues: CreateGrades("+150", "+165", "+180", "+225"),
                            Description: "The Revelation Mastery bonus counts towards the promotion points distributed in the domain of the corresponding Revelation Perk."),
                        new WheelPlannerModSnapshot(
                            VariantKey: "official-supreme-6",
                            Name: "Avatar of Steel - Cooldown / Momentum",
                            Type: GemModifierType.Supreme,
                            Category: GemModifierCategory.VocationSpecific,
                            VocationRestriction: null,
                            GradeValues: CreateGrades("-900s", "-900s / +0.33%", "-900s / +0.66%", "-900s / +1%"),
                            Description: null)
                    ]));
            }
        }

        private sealed class FakeTibiaWikiHttpService : ITibiaWikiHttpService
        {
            public Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                if (requestUri.Contains("Basic%20Mod", StringComparison.Ordinal))
                {
                    return Task.FromResult(BuildApiResponse(BasicModWikiText));
                }

                if (requestUri.Contains("Supreme%20Mod", StringComparison.Ordinal))
                {
                    return Task.FromResult(BuildApiResponse(SupremeModWikiText));
                }

                throw new InvalidOperationException($"Unexpected TibiaWiki request URI '{requestUri}'.");
            }

            public Task<byte[]> GetBytesAsync(string requestUri, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            private static string BuildApiResponse(string content)
            {
                return JsonSerializer.Serialize(new
                {
                    query = new
                    {
                        pages = new[]
                        {
                            new
                            {
                                revisions = new[]
                                {
                                    new
                                    {
                                        slots = new
                                        {
                                            main = new
                                            {
                                                content
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }
        }

        private static Dictionary<GemGrade, string> CreateGrades(string gradeI, string gradeIi, string gradeIii, string gradeIv)
        {
            return new Dictionary<GemGrade, string>
            {
                [GemGrade.GradeI] = gradeI,
                [GemGrade.GradeII] = gradeIi,
                [GemGrade.GradeIII] = gradeIii,
                [GemGrade.GradeIV] = gradeIv
            };
        }

        private static TibiaDbContext CreateDbContext()
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            TibiaDbContext db = new(options);
            db.Database.OpenConnection();
            db.Database.EnsureCreated();

            return db;
        }

        private const string BasicModWikiText = """
            == General ==
            {|class="wikitable"
            ! Mod !! Grade I !! Grade II !! Grade III !! Grade IV
            |-
            | [[Fire Damage|Fire Resistance]] / [[Earth Damage|Earth Resistance]] || +1% / +1% || +1.1% / +1.1% || +1.2% / +1.2% || +1.5% / +1.5%
            |-
            | [[Fire Damage|Fire Resistance]] / [[Earth Damage|Earth Resistance]] || +3% / -2% || +3.3% / -2% || +3.6% / -2% || +4.5% / -2%
            |-
            | [[Holy Damage|Holy Resistance]] / [[Death Damage|Death Resistance]] || +1.5% / -1% || +1.65% / -1% || +1.8% / -1% || +2.25% / -1%
            |}

            == Mages ==
            {|class="wikitable"
            ! Mod !! Grade I !! Grade II !! Grade III !! Grade IV
            |-
            | [[Mana]] / [[Fire Damage|Fire Resistance]] || +300 / +1% || +330 / +1.1% || +360 / +1.2% || +450 / +1.5%
            |}

            == Monks ==
            {|class="wikitable"
            ! Mod !! Grade I !! Grade II !! Grade III !! Grade IV
            |-
            | [[Hit Points]] || ? || ? || ? || 300
            |}
            """;

        private const string SupremeModWikiText = """
            == General ==
            {|class="wikitable"
            ! [[Augments]] !! Grade I !! Grade II !! Grade III !! Grade IV
            |-
            | [[Dodge]] || +0.28% || +0.31% || +0.34% || +0.42%
            |}

            == Knights ==
            {|class="wikitable"
            ! [[Augments]] !! Grade I !! Grade II !! Grade III !! Grade IV
            |-
            | [[Avatar of Steel]] - [[Cooldown]] / [[Momentum]] || -900s || -900s / +0.33% || -900s / +0.66% || -900s / +1%
            |-
            | Revelation Mastery [[Gift of Life]] - [[Promotion Point]]s || +150 || +165 || +180 || +225
            |}

            == Druids ==
            {|class="wikitable"
            ! [[Augments]] !! Grade I !! Grade II !! Grade III !! Grade IV
            |-
            | [[Ultimate Healing]] - Base Healing || +5% || +5.5% || +6% || +7.5%
            |}

            == Sorcerers ==
            {|class="wikitable"
            ! [[Augments]] !! Grade I !! Grade II !! Grade III !! Grade IV
            |-
            | [[Ultimate Healing]] - Base Healing || +5% || +5.5% || +6% || +7.5%
            |}

            == Monks ==
            {|class="wikitable"
            ! [[]] !! Grade I !! Grade II !! Grade III !! Grade IV
            |-
            | [[Spirit Mend]] - Base Healing || ? || ? || ? || +9%
            |}
            """;
    }
}
