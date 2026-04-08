namespace TibiaDataApi.Services.Categories
{
    public sealed record BosstiaryLevelDefinition(
        int Level,
        string Name,
        int KillsRequired,
        int PointsAwarded);

    public sealed record BosstiaryCategoryDefinition(
        string Slug,
        string Name,
        int SortOrder,
        IReadOnlyList<BosstiaryLevelDefinition> LevelRequirements)
    {
        public int GetTotalKillsRequired()
        {
            return LevelRequirements.Sum(entry => entry.KillsRequired);
        }

        public int GetTotalPoints()
        {
            return LevelRequirements.Sum(entry => entry.PointsAwarded);
        }
    }

    public static class BosstiaryCatalog
    {
        public static IReadOnlyList<BosstiaryCategoryDefinition> Categories { get; } =
        [
            new(
                "bane",
                "Bane",
                1,
                CreateLevels(25, 100, 300, 5, 15, 30)),
            new(
                "archfoe",
                "Archfoe",
                2,
                CreateLevels(5, 20, 60, 10, 30, 60)),
            new(
                "nemesis",
                "Nemesis",
                3,
                CreateLevels(1, 3, 5, 10, 30, 60))
        ];

        public static BosstiaryCategoryDefinition GetRequiredCategory(string slug)
        {
            BosstiaryCategoryDefinition? definition = Categories.FirstOrDefault(entry =>
                string.Equals(entry.Slug, slug, StringComparison.OrdinalIgnoreCase));

            return definition ?? throw new InvalidOperationException(
                $"No bosstiary category definition was found for slug '{slug}'.");
        }

        private static IReadOnlyList<BosstiaryLevelDefinition> CreateLevels(
            int firstLevelKills,
            int secondLevelKills,
            int thirdLevelKills,
            int firstLevelPoints,
            int secondLevelPoints,
            int thirdLevelPoints)
        {
            return
            [
                new(1, "First", firstLevelKills, firstLevelPoints),
                new(2, "Second", secondLevelKills, secondLevelPoints),
                new(3, "Final", thirdLevelKills, thirdLevelPoints)
            ];
        }
    }
}
