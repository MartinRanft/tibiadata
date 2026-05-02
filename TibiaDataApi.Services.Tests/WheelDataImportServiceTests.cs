using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Entities.WheelOfDestiny;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.WheelOfDestiny;
using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.Tests
{
    public sealed class WheelDataImportServiceTests
    {
        [Fact]
        public async Task ImportAsync_RebuildsWheelPerksFromStoredWikiArticles()
        {
            await using TibiaDbContext db = CreateDbContext();

            SeedArticle(
                db,
                WikiContentType.WikiPage,
                "Wheel of Destiny/Dedication Perks",
                """
                '''Dedication Perks''' intro text.
                * '''Hit Points''': Increases maximum hit points.
                * '''Mana''': Increases maximum mana.
                * '''Mitigation Multiplier''': Reduces incoming damage.
                """);

            SeedArticle(
                db,
                WikiContentType.WikiPage,
                "Wheel of Destiny/Conviction Perks",
                """
                == Generic Conviction Perks ==
                * '''Resistance to Fire''': Grants 2% fire protection

                == Elite Knight ==
                === Battle Instinct ===
                Gain more shielding.

                === Augmentations ===
                {| class="wikitable"
                |-
                ! Ability !! Stage 1 !! Stage 2
                |-
                | [[File:Augmented Fierce Beserk Icon.gif|left]] [[Fierce Berserk]] || -30 Mana Cost || +10% Base Damage
                |}

                == Mages ==
                === Runic Mastery ===
                Rune usage can empower your magic level.
                """);

            SeedArticle(
                db,
                WikiContentType.WikiPage,
                "Wheel of Destiny/Revelation Perks",
                """
                == All vocations ==
                === Increased Damage and Healing ===
                Grants extra damage and healing.
                {| class="wikitable"
                |-
                ! Passive !! Stage 1 !! Stage 2 !! Stage 3
                |-
                | Damage and healing increase || 4 || 9 || 20
                |}

                === Gift of Life ===
                Prevents otherwise fatal damage.

                === Avatar ===
                Turns the player into an avatar.

                == Elite Knights ==
                === Combat Mastery ===
                Improves shields and critical damage.
                {| class="wikitable"
                |-
                ! Combat Mastery !! Stage 1 !! Stage 2 !! Stage 3
                |-
                | Additional shield defence || 10 || 20 || 30
                |}

                == Royal Paladins ==
                === Divine Grenade ===
                Plants a delayed holy explosion.

                == Master Sorcerer ==
                === Beam Mastery ===
                Unlocks a powerful beam bonus.
                {| class="wikitable"
                |-
                ! Beam Mastery !! Stage 1 !! Stage 2 !! Stage 3
                |-
                | Damage increase of beam spells || 10% || 12% || 14%
                |}

                == Exalted Monk ==
                === Spiritual Outburst ===
                Releases a chaining attack.

                === Ascetic ===
                Improve all spenders. Increases the Harmony base bonus by 1%/2%/3% and your auto attacks deal additional damage equal to 100%/200%/300% of your mantra.
                """);

            SeedArticle(
                db,
                WikiContentType.Spell,
                "Gift of Life",
                """
                {{Infobox Spell
                | effect = Prevents otherwise fatal damage.
                | notes = Restores hit points before fatal damage.
                {{{!}} class="wikitable"
                {{!}}-
                ! Gift of Life !! Stage 1 !! Stage 2 !! Stage 3
                {{!}}-
                {{!}} Overkill damage {{!}}{{!}} 20% {{!}}{{!}} 25% {{!}}{{!}} 30%
                {{!}}}
                }}
                """);

            SeedArticle(
                db,
                WikiContentType.Spell,
                "Avatar of Steel",
                """
                {{Infobox Spell
                | effect = Turns the player into an avatar.
                | notes = Grants damage reduction and critical hits.
                {{{!}} class="wikitable"
                {{!}}-
                ! Avatar !! Stage 1 !! Stage 2 !! Stage 3
                {{!}}-
                {{!}} Additional damage reduction {{!}}{{!}} 5% {{!}}{{!}} 10% {{!}}{{!}} 15%
                {{!}}}
                }}
                """);

            SeedArticle(
                db,
                WikiContentType.Spell,
                "Avatar of Light",
                """
                {{Infobox Spell
                | effect = Turns the player into an avatar.
                | notes = Grants damage reduction and critical hits.
                {{{!}} class="wikitable"
                {{!}}-
                ! Avatar !! Stage 1 !! Stage 2 !! Stage 3
                {{!}}-
                {{!}} Additional damage reduction {{!}}{{!}} 5% {{!}}{{!}} 10% {{!}}{{!}} 15%
                {{!}}}
                }}
                """);

            SeedArticle(
                db,
                WikiContentType.Spell,
                "Avatar of Nature",
                """
                {{Infobox Spell
                | effect = Turns the player into an avatar.
                | notes = Grants damage reduction and critical hits.
                {{{!}} class="wikitable"
                {{!}}-
                ! Avatar !! Stage 1 !! Stage 2 !! Stage 3
                {{!}}-
                {{!}} Additional damage reduction {{!}}{{!}} 5% {{!}}{{!}} 10% {{!}}{{!}} 15%
                {{!}}}
                }}
                """);

            SeedArticle(
                db,
                WikiContentType.Spell,
                "Avatar of Storm",
                """
                {{Infobox Spell
                | effect = Turns the player into an avatar.
                | notes = Grants damage reduction and critical hits.
                {{{!}} class="wikitable"
                {{!}}-
                ! Avatar !! Stage 1 !! Stage 2 !! Stage 3
                {{!}}-
                {{!}} Additional damage reduction {{!}}{{!}} 5% {{!}}{{!}} 10% {{!}}{{!}} 15%
                {{!}}}
                }}
                """);

            SeedArticle(
                db,
                WikiContentType.Spell,
                "Avatar of Balance",
                """
                {{Infobox Spell
                | effect = Turns the player into an avatar.
                | notes = Grants damage reduction and critical hits.
                {{{!}} class="wikitable"
                {{!}}-
                ! Avatar !! Stage 1 !! Stage 2 !! Stage 3
                {{!}}-
                {{!}} Additional damage reduction {{!}}{{!}} 5% {{!}}{{!}} 10% {{!}}{{!}} 15%
                {{!}}}
                }}
                """);

            SeedArticle(
                db,
                WikiContentType.Spell,
                "Divine Grenade",
                """
                {{Infobox Spell
                | effect = Plants a delayed holy explosion.
                | notes = Higher stages reduce cooldown.
                {{{!}} class="wikitable"
                {{!}}-
                ! Divine Grenade !! Stage 1 !! Stage 2 !! Stage 3
                {{!}}-
                {{!}} Cooldown {{!}}{{!}} 26 seconds {{!}}{{!}} 20 seconds {{!}}{{!}} 14 seconds
                {{!}}}
                }}
                """);

            SeedArticle(
                db,
                WikiContentType.Spell,
                "Spiritual Outburst",
                """
                {{Infobox Spell
                | effect = Releases a chaining attack.
                | notes = Higher stages improve repeat damage.
                {{{!}} class="wikitable"
                {{!}}-
                ! Spiritual Outburst !! Stage 1 !! Stage 2 !! Stage 3
                {{!}}-
                {{!}} Repeat damage {{!}}{{!}} 37.5% {{!}}{{!}} 50% {{!}}{{!}} 62.5%
                {{!}}}
                }}
                """);

            db.WheelPerks.Add(new WheelPerk
            {
                Key = "elite-knight:revelation:legacy",
                Slug = "legacy",
                Vocation = WheelVocation.EliteKnight,
                Type = WheelPerkType.Revelation,
                Name = "Legacy",
                IsActive = true,
                LastUpdated = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            WheelDataImportService service = new(
                NullLogger<WheelDataImportService>.Instance,
                new StubWheelPlannerLayoutSource(CreatePlannerLayoutSnapshot()),
                CreateStubGemModDataImportService());

            WheelDataImportResult result = await service.ImportAsync(db);

            Assert.Equal(11, result.SourceArticleCount);
            Assert.True(result.PerksProcessed > 0);
            Assert.True(result.Added > 0);
            Assert.Equal(1, result.Removed);

            List<WheelPerk> perks = await db.WheelPerks
                                            .Include(entry => entry.Occurrences)
                                            .Include(entry => entry.Stages)
                                            .OrderBy(entry => entry.Key)
                                            .ToListAsync();

            Assert.DoesNotContain(perks, entry => entry.Name == "Legacy");

            Assert.Equal(5, perks.Count(entry => entry.Type == WheelPerkType.Dedication && entry.Name == "Hit Points"));
            Assert.Contains(perks, entry =>
                entry.Name == "Runic Mastery" &&
                entry.Vocation == WheelVocation.ElderDruid &&
                entry.IsGenericAcrossVocations);
            Assert.Contains(perks, entry =>
                entry.Name == "Runic Mastery" &&
                entry.Vocation == WheelVocation.MasterSorcerer &&
                entry.IsGenericAcrossVocations);

            WheelPerk battleInstinct = Assert.Single(
                perks,
                entry => entry.Name == "Battle Instinct" &&
                         entry.Vocation == WheelVocation.EliteKnight);
            Assert.Single(battleInstinct.Occurrences);
            Assert.True(battleInstinct.Occurrences[0].IsStackable);
            Assert.Equal((short?)25, battleInstinct.Occurrences[0].RequiredPoints);

            WheelPerk fierceBerserk = Assert.Single(
                perks,
                entry => entry.Name == "Fierce Berserk" &&
                         entry.Vocation == WheelVocation.EliteKnight);
            Assert.Equal(2, fierceBerserk.Occurrences.Count);
            Assert.Equal(2, fierceBerserk.Stages.Count);
            Assert.Equal(WheelStageUnlockKind.OccurrenceCount, fierceBerserk.Stages[0].UnlockKind);
            Assert.Equal((short?)25, fierceBerserk.Occurrences[0].RequiredPoints);
            Assert.Equal((short?)50, fierceBerserk.Occurrences[1].RequiredPoints);

            WheelPerk giftOfLife = Assert.Single(
                perks,
                entry => entry.Name == "Gift of Life" &&
                         entry.Vocation == WheelVocation.RoyalPaladin);
            Assert.Equal("Gift of Life", giftOfLife.MainSourceTitle);
            Assert.Equal(3, giftOfLife.Stages.Count);
            Assert.Equal((short?)250, giftOfLife.Occurrences[0].RequiredPoints);
            Assert.Equal(250, giftOfLife.Stages[0].UnlockValue);

            WheelPerk avatarKnight = Assert.Single(
                perks,
                entry => entry.Name == "Avatar" &&
                         entry.Vocation == WheelVocation.EliteKnight);
            Assert.Equal("Avatar of Steel", avatarKnight.MainSourceTitle);
            Assert.Equal(3, avatarKnight.Stages.Count);

            WheelPerk divineGrenade = Assert.Single(
                perks,
                entry => entry.Name == "Divine Grenade" &&
                         entry.Vocation == WheelVocation.RoyalPaladin);
            Assert.Equal("Divine Grenade", divineGrenade.MainSourceTitle);
            Assert.Equal(3, divineGrenade.Stages.Count);

            WheelPerk combatMastery = Assert.Single(
                perks,
                entry => entry.Name == "Combat Mastery" &&
                         entry.Vocation == WheelVocation.EliteKnight);
            Assert.Equal("Wheel of Destiny/Revelation Perks", combatMastery.MainSourceTitle);
            Assert.Equal(3, combatMastery.Stages.Count);

            WheelPerk increasedDamage = Assert.Single(
                perks,
                entry => entry.Name == "Increased Damage and Healing" &&
                         entry.Vocation == WheelVocation.ExaltedMonk);
            Assert.Equal(3, increasedDamage.Stages.Count);

            WheelPerk ascetic = Assert.Single(
                perks,
                entry => entry.Name == "Ascetic" &&
                         entry.Vocation == WheelVocation.ExaltedMonk);
            Assert.Equal(3, ascetic.Stages.Count);
            Assert.Contains("Harmony base bonus", ascetic.Stages[0].EffectSummary ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("auto attacks deal additional damage", ascetic.Stages[2].EffectSummary ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            WheelPerk hitPoints = Assert.Single(
                perks,
                entry => entry.Name == "Hit Points" &&
                         entry.Vocation == WheelVocation.EliteKnight);
            Assert.Equal((short?)1, hitPoints.Occurrences[0].RequiredPoints);

            List<WheelSection> sections = await db.WheelSections
                                                 .Include(entry => entry.DedicationPerks)
                                                 .ThenInclude(entry => entry.WheelPerk)
                                                 .Include(entry => entry.ConvictionWheelPerk)
                                                 .Include(entry => entry.ConvictionWheelPerkOccurrence)
                                                 .OrderBy(entry => entry.Vocation)
                                                 .ThenBy(entry => entry.SectionKey)
                                                 .ToListAsync();

            Assert.Equal(3, sections.Count);

            WheelSection battleInstinctSection = Assert.Single(
                sections,
                entry => entry.Vocation == WheelVocation.EliteKnight &&
                         entry.SectionKey == "QTL8");
            Assert.Equal((short)200, battleInstinctSection.SectionPoints);
            Assert.Equal("Battle Instinct", battleInstinctSection.ConvictionWheelPerk.Name);
            Assert.NotNull(battleInstinctSection.ConvictionWheelPerkOccurrence);
            Assert.Equal((short)1, battleInstinctSection.ConvictionWheelPerkOccurrence!.OccurrenceIndex);
            Assert.Equal(
                ["Hit Points", "Mana"],
                battleInstinctSection.DedicationPerks
                                     .OrderBy(entry => entry.SortOrder)
                                     .Select(entry => entry.WheelPerk.Name)
                                     .ToList());

            List<WheelSection> fierceBerserkSections = sections
                                                       .Where(entry => entry.ConvictionWheelPerk.Name == "Fierce Berserk")
                                                       .OrderBy(entry => entry.SectionPoints)
                                                       .ToList();

            Assert.Equal(2, fierceBerserkSections.Count);
            Assert.Equal((short)1, fierceBerserkSections[0].ConvictionWheelPerkOccurrence?.OccurrenceIndex);
            Assert.Equal((short)2, fierceBerserkSections[1].ConvictionWheelPerkOccurrence?.OccurrenceIndex);

            List<WheelRevelationSlot> revelationSlots = await db.WheelRevelationSlots
                                                                 .Include(entry => entry.WheelPerk)
                                                                 .Include(entry => entry.WheelPerkOccurrence)
                                                                 .OrderBy(entry => entry.Vocation)
                                                                 .ThenBy(entry => entry.SlotKey)
                                                                 .ToListAsync();

            Assert.Equal(2, revelationSlots.Count);

            WheelRevelationSlot avatarSlot = Assert.Single(
                revelationSlots,
                entry => entry.Vocation == WheelVocation.EliteKnight &&
                         entry.SlotKey == "QBR");
            Assert.Equal((short)250, avatarSlot.RequiredPoints);
            Assert.Equal("Avatar", avatarSlot.WheelPerk.Name);
            Assert.NotNull(avatarSlot.WheelPerkOccurrence);

            WheelRevelationSlot asceticSlot = Assert.Single(
                revelationSlots,
                entry => entry.Vocation == WheelVocation.ExaltedMonk &&
                         entry.SlotKey == "QBL");
            Assert.Equal("Ascetic", asceticSlot.WheelPerk.Name);
            Assert.NotNull(asceticSlot.WheelPerkOccurrence);
        }

        [Fact]
        public async Task ImportAsync_MapsOfficialPlannerNamesToCurrentWikiCatalog()
        {
            await using TibiaDbContext db = CreateDbContext();

            SeedArticle(
                db,
                WikiContentType.WikiPage,
                "Wheel of Destiny/Dedication Perks",
                """
                * '''Hit Points''': Increases maximum hit points.
                * '''Mana''': Increases maximum mana.
                * '''Capacity''': Increases maximum capacity.
                * '''Mitigation''': Multiplicatively increases your mitigation.
                """);

            SeedArticle(
                db,
                WikiContentType.WikiPage,
                "Wheel of Destiny/Conviction Perks",
                """
                == Generic Conviction Perks ==
                * '''Weapon, Distance, Magic Skill Boosts''': Grants a boost to the main offensive skill of your vocation
                * '''Vessel Resonance''': Grants +1 damage/healing bonus if a gem size matches the Vessel Resonance in a domain.
                """);

            SeedArticle(
                db,
                WikiContentType.WikiPage,
                "Wheel of Destiny/Revelation Perks",
                "No revelation perks needed for this test.");

            await db.SaveChangesAsync();

            WheelDataImportService service = new(
                NullLogger<WheelDataImportService>.Instance,
                new StubWheelPlannerLayoutSource(
                    new WheelPlannerLayoutSnapshot(
                    [
                        new WheelPlannerSectionSnapshot(
                            WheelVocation.EliteKnight,
                            "QTL1",
                            WheelQuarter.TopLeft,
                            1,
                            2,
                            75,
                            "+0 Hit Points",
                            """
                            +1 Weapon Skill Boost
                            Applies to sword, axe and club fighting
                            """),
                        new WheelPlannerSectionSnapshot(
                            WheelVocation.EliteKnight,
                            "QTL5",
                            WheelQuarter.TopLeft,
                            3,
                            7,
                            150,
                            "0% Mitigation Multiplier",
                            "Vessel Resonance Top Left"),
                        new WheelPlannerSectionSnapshot(
                            WheelVocation.EliteKnight,
                            "QTL0",
                            WheelQuarter.TopLeft,
                            0,
                            1,
                            50,
                            "+0 Capacity",
                            "Vessel Resonance Top Left"),
                        new WheelPlannerSectionSnapshot(
                            WheelVocation.EliteKnight,
                            "QTL2",
                            WheelQuarter.TopLeft,
                            2,
                            4,
                            100,
                            "+0 Hit Points",
                            "Vessel Resonance Top Left"),
                        new WheelPlannerSectionSnapshot(
                            WheelVocation.EliteKnight,
                            "QTL3",
                            WheelQuarter.TopLeft,
                            1,
                            3,
                            75,
                            "+0 Mana",
                            "Vessel Resonance Top Left"),
                        new WheelPlannerSectionSnapshot(
                            WheelVocation.RoyalPaladin,
                            "QTL1",
                            WheelQuarter.TopLeft,
                            1,
                            2,
                            75,
                            "+0 Hit Points",
                            "+1 Distance Skill Boost"),
                        new WheelPlannerSectionSnapshot(
                            WheelVocation.ElderDruid,
                            "QTL1",
                            WheelQuarter.TopLeft,
                            1,
                            2,
                            75,
                            "+0 Hit Points",
                            "+1 Magic Skill Boost"),
                        new WheelPlannerSectionSnapshot(
                            WheelVocation.MasterSorcerer,
                            "QTL1",
                            WheelQuarter.TopLeft,
                            1,
                            2,
                            75,
                            "+0 Hit Points",
                            "+1 Magic Skill Boost"),
                        new WheelPlannerSectionSnapshot(
                            WheelVocation.ExaltedMonk,
                            "QTL1",
                            WheelQuarter.TopLeft,
                            1,
                            2,
                            75,
                            "+0 Hit Points",
                            "+1 Fist Fighting Skill Boost")
                    ],
                    [])),
                CreateStubGemModDataImportService());

            await service.ImportAsync(db);

            List<WheelSection> sections = await db.WheelSections
                                                 .Include(entry => entry.DedicationPerks)
                                                 .ThenInclude(entry => entry.WheelPerk)
                                                 .Include(entry => entry.ConvictionWheelPerk)
                                                 .OrderBy(entry => entry.Vocation)
                                                 .ThenBy(entry => entry.SectionKey)
                                                 .ToListAsync();

            Assert.Equal(9, sections.Count);
            Assert.Equal(
                "Capacity",
                Assert.Single(
                    sections,
                    entry => entry.Vocation == WheelVocation.EliteKnight &&
                             entry.SectionKey == "QTL0").DedicationPerks.Single().WheelPerk.Name);
            Assert.Equal(
                "Hit Points",
                Assert.Single(
                    sections,
                    entry => entry.Vocation == WheelVocation.EliteKnight &&
                             entry.SectionKey == "QTL2").DedicationPerks.Single().WheelPerk.Name);
            Assert.Equal(
                "Mana",
                Assert.Single(
                    sections,
                    entry => entry.Vocation == WheelVocation.EliteKnight &&
                             entry.SectionKey == "QTL3").DedicationPerks.Single().WheelPerk.Name);
            Assert.Equal(
                "Mitigation",
                Assert.Single(
                    sections,
                    entry => entry.Vocation == WheelVocation.EliteKnight &&
                             entry.SectionKey == "QTL5").DedicationPerks.Single().WheelPerk.Name);
            Assert.Equal(
                "Weapon, Distance, Magic Skill Boosts",
                Assert.Single(
                    sections,
                    entry => entry.Vocation == WheelVocation.EliteKnight &&
                             entry.SectionKey == "QTL1").ConvictionWheelPerk.Name);
            Assert.Equal(
                "Weapon, Distance, Magic Skill Boosts",
                Assert.Single(sections, entry => entry.Vocation == WheelVocation.RoyalPaladin).ConvictionWheelPerk.Name);
            Assert.Equal(
                "Weapon, Distance, Magic Skill Boosts",
                Assert.Single(sections, entry => entry.Vocation == WheelVocation.ElderDruid).ConvictionWheelPerk.Name);
            Assert.Equal(
                "Weapon, Distance, Magic Skill Boosts",
                Assert.Single(sections, entry => entry.Vocation == WheelVocation.MasterSorcerer).ConvictionWheelPerk.Name);
            Assert.Equal(
                "Weapon, Distance, Magic Skill Boosts",
                Assert.Single(sections, entry => entry.Vocation == WheelVocation.ExaltedMonk).ConvictionWheelPerk.Name);
        }

        private static TibiaDbContext CreateDbContext()
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                                                       .Options;

            return new TibiaDbContext(options);
        }

        private static void SeedArticle(
            TibiaDbContext db,
            WikiContentType contentType,
            string title,
            string rawWikiText)
        {
            db.WikiArticles.Add(new WikiArticle
            {
                ContentType = contentType,
                Title = title,
                NormalizedTitle = title.ToLowerInvariant(),
                Summary = title,
                PlainTextContent = rawWikiText,
                RawWikiText = rawWikiText,
                WikiUrl = $"https://tibia.fandom.com/wiki/{Uri.EscapeDataString(title.Replace(' ', '_'))}",
                LastSeenAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsMissingFromSource = false
            });
        }

        private static WheelPlannerLayoutSnapshot CreatePlannerLayoutSnapshot()
        {
            return new WheelPlannerLayoutSnapshot(
            [
                new WheelPlannerSectionSnapshot(
                    WheelVocation.EliteKnight,
                    "QTL8",
                    WheelQuarter.TopLeft,
                    4,
                    9,
                    200,
                    "+0 Hit Points\n+0 Mana",
                    """
                    Battle Instinct
                    Gain +6 shielding and +1 sword / axe / club fighting when 5 creatures are on adjacent squares.
                    """),
                new WheelPlannerSectionSnapshot(
                    WheelVocation.EliteKnight,
                    "QTR0",
                    WheelQuarter.TopRight,
                    0,
                    1,
                    50,
                    "0% Mitigation Multiplier",
                    """
                    Augmented Fierce Berserk

                    :
                    -30 Mana Cost

                    :
                    +10% Base Damage
                    """),
                new WheelPlannerSectionSnapshot(
                    WheelVocation.EliteKnight,
                    "QBL8",
                    WheelQuarter.BottomLeft,
                    4,
                    9,
                    200,
                    "+0 Hit Points\n+0 Mana",
                    """
                    Augmented Fierce Berserk

                    :
                    -30 Mana Cost

                    :
                    +10% Base Damage
                    """)
            ],
            [
                new WheelPlannerRevelationSlotSnapshot(
                    WheelVocation.EliteKnight,
                    "QBR",
                    WheelQuarter.BottomRight,
                    250,
                    "Avatar of Steel"),
                new WheelPlannerRevelationSlotSnapshot(
                    WheelVocation.ExaltedMonk,
                    "QBL",
                    WheelQuarter.BottomLeft,
                    250,
                    "Ascetic")
            ]);
        }

        private static IGemModDataImportService CreateStubGemModDataImportService()
        {
            return new StubGemModDataImportService();
        }

        private sealed class StubWheelPlannerLayoutSource(WheelPlannerLayoutSnapshot snapshot) : IWheelPlannerLayoutSource
        {
            public Task<WheelPlannerLayoutSnapshot> LoadAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(snapshot);
            }

            public Task<WheelPlannerFullSnapshot> LoadFullAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new WheelPlannerFullSnapshot(snapshot, [], []));
            }
        }

        private sealed class StubGemModDataImportService : IGemModDataImportService
        {
            public Task<GemImportResult> ImportGemsAsync(TibiaDbContext db, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new GemImportResult(
                    SourcePageCount: 0,
                    GemsProcessed: 0,
                    ModifiersProcessed: 0,
                    Added: 0,
                    Updated: 0,
                    Unchanged: 0,
                    Removed: 0));
            }
        }
    }
}
