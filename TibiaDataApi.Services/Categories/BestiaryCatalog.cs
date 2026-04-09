namespace TibiaDataApi.Services.Categories
{
    public enum BestiaryOccurrence
    {
        Ordinary = 1,
        VeryRare = 2
    }

    public sealed record BestiaryLevelDefinition(
        int Level,
        string Name,
        int KillsRequired);

    public sealed record BestiaryClassDefinition(
        string Slug,
        string Name,
        int SortOrder);

    public sealed record BestiaryDifficultyDefinition(
        string Slug,
        string Name,
        int SortOrder,
        int OrdinaryCharmPoints,
        int VeryRareCharmPoints,
        IReadOnlyList<BestiaryLevelDefinition> OrdinaryLevelRequirements,
        IReadOnlyList<BestiaryLevelDefinition> VeryRareLevelRequirements)
    {
        public IReadOnlyList<BestiaryLevelDefinition> GetLevelRequirements(BestiaryOccurrence occurrence)
        {
            return occurrence == BestiaryOccurrence.VeryRare
                ? VeryRareLevelRequirements
                : OrdinaryLevelRequirements;
        }

        public int GetCharmPoints(BestiaryOccurrence occurrence)
        {
            return occurrence == BestiaryOccurrence.VeryRare
                ? VeryRareCharmPoints
                : OrdinaryCharmPoints;
        }

        public int GetTotalKillsRequired(BestiaryOccurrence occurrence)
        {
            return GetLevelRequirements(occurrence).Sum(entry => entry.KillsRequired);
        }
    }

    public static class BestiaryCatalog
    {
        public static IReadOnlyList<BestiaryClassDefinition> Classes { get; } =
        [
            new("amphibic", "Amphibic", 1),
            new("aquatic", "Aquatic", 2),
            new("bird", "Bird", 3),
            new("construct", "Construct", 4),
            new("demon", "Demon", 5),
            new("dragon", "Dragon", 6),
            new("elemental", "Elemental", 7),
            new("extra-dimensional", "Extra Dimensional", 8),
            new("fey", "Fey", 9),
            new("giant", "Giant", 10),
            new("human", "Human", 11),
            new("humanoid", "Humanoid", 12),
            new("lycanthrope", "Lycanthrope", 13),
            new("magical", "Magical", 14),
            new("mammal", "Mammal", 15),
            new("plant", "Plant", 16),
            new("reptile", "Reptile", 17),
            new("slime", "Slime", 18),
            new("undead", "Undead", 19),
            new("vermin", "Vermin", 20)
        ];

        
        public static IReadOnlyList<BestiaryClassDefinition> Categories => Classes;

        public static IReadOnlyList<BestiaryDifficultyDefinition> Difficulties { get; } =
        [
            new(
                "harmless",
                "Harmless",
                1,
                1,
                5,
                CreateLevelRequirements(5, 10, 25),
                CreateLevelRequirements(2, 3, 5)),
            new(
                "trivial",
                "Trivial",
                2,
                5,
                10,
                CreateLevelRequirements(10, 100, 250),
                CreateLevelRequirements(2, 3, 5)),
            new(
                "easy",
                "Easy",
                3,
                15,
                30,
                CreateLevelRequirements(25, 250, 500),
                CreateLevelRequirements(2, 3, 5)),
            new(
                "medium",
                "Medium",
                4,
                25,
                50,
                CreateLevelRequirements(50, 500, 1000),
                CreateLevelRequirements(2, 3, 5)),
            new(
                "hard",
                "Hard",
                5,
                50,
                100,
                CreateLevelRequirements(100, 1000, 2500),
                CreateLevelRequirements(2, 3, 5)),
            new(
                "challenging",
                "Challenging",
                6,
                100,
                200,
                CreateLevelRequirements(200, 2000, 5000),
                CreateLevelRequirements(2, 3, 5))
        ];

        public static BestiaryClassDefinition GetRequiredClass(string slug)
        {
            BestiaryClassDefinition? definition = Classes.FirstOrDefault(entry =>
                string.Equals(entry.Slug, slug, StringComparison.OrdinalIgnoreCase));

            return definition ?? throw new InvalidOperationException(
                $"No bestiary class definition was found for slug '{slug}'.");
        }

        public static BestiaryClassDefinition GetRequiredCategory(string slug)
        {
            return GetRequiredClass(slug);
        }

        public static BestiaryDifficultyDefinition GetRequiredDifficulty(string slug)
        {
            BestiaryDifficultyDefinition? definition = Difficulties.FirstOrDefault(entry =>
                string.Equals(entry.Slug, slug, StringComparison.OrdinalIgnoreCase));

            return definition ?? throw new InvalidOperationException(
                $"No bestiary difficulty definition was found for slug '{slug}'.");
        }

        private static IReadOnlyList<BestiaryLevelDefinition> CreateLevelRequirements(
            int firstLevelKills,
            int secondLevelKills,
            int thirdLevelKills)
        {
            return
            [
                new(1, "First", firstLevelKills),
                new(2, "Second", secondLevelKills),
                new(3, "Final", thirdLevelKills)
            ];
        }
    }
}
