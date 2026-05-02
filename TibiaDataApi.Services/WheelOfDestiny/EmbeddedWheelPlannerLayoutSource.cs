using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.WheelOfDestiny
{
    public sealed class EmbeddedWheelPlannerLayoutSource(
        ILogger<EmbeddedWheelPlannerLayoutSource> logger) : IWheelPlannerLayoutSource
    {
        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };

        internal const string SnapshotFileName = "wheel-planner-snapshot.json";
        internal const string EmbeddedSnapshotResourceName = "TibiaDataApi.Services.WheelOfDestiny.Seed.wheel-planner-snapshot.json";

        public async Task<WheelPlannerLayoutSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            WheelPlannerFullSnapshot snapshot = await LoadSnapshotAsync(cancellationToken);
            return snapshot.Layout;
        }

        public async Task<WheelPlannerFullSnapshot> LoadFullAsync(CancellationToken cancellationToken = default)
        {
            return await LoadSnapshotAsync(cancellationToken);
        }

        private async Task<WheelPlannerFullSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            string? overridePath = Environment.GetEnvironmentVariable("WHEEL_SEED_SAVE_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                string filePath = Path.Combine(overridePath, SnapshotFileName);
                if (File.Exists(filePath))
                {
                    return await LoadFromFileAsync(filePath, cancellationToken);
                }
            }

            return await LoadFromEmbeddedResourceAsync(cancellationToken);
        }

        private async Task<WheelPlannerFullSnapshot> LoadFromFileAsync(string path, CancellationToken cancellationToken)
        {
            await using FileStream stream = File.OpenRead(path);
            WheelPlannerFullSnapshot? snapshot = await JsonSerializer.DeserializeAsync<WheelPlannerFullSnapshot>(
                stream, JsonOptions, cancellationToken);

            if (snapshot is null)
            {
                logger.LogWarning("Wheel seed file at '{Path}' could not be deserialized.", path);
                return new WheelPlannerFullSnapshot(WheelPlannerLayoutSnapshot.Empty, [], []);
            }

            logger.LogInformation(
                "Loaded wheel planner snapshot from file: {SectionCount} sections, {SlotCount} slots, {GemCount} gems, {ModCount} mods.",
                snapshot.Layout.Sections.Count,
                snapshot.Layout.RevelationSlots.Count,
                snapshot.Gems.Count,
                snapshot.Mods.Count);

            return snapshot;
        }

        private async Task<WheelPlannerFullSnapshot> LoadFromEmbeddedResourceAsync(CancellationToken cancellationToken)
        {
            Stream? resourceStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(EmbeddedSnapshotResourceName);

            if (resourceStream is null)
            {
                logger.LogWarning(
                    "Embedded wheel planner snapshot resource '{Name}' not found. Run the wheel import in Development to generate it.",
                    EmbeddedSnapshotResourceName);
                return new WheelPlannerFullSnapshot(WheelPlannerLayoutSnapshot.Empty, [], []);
            }

            await using Stream _ = resourceStream;
            WheelPlannerFullSnapshot? snapshot = await JsonSerializer.DeserializeAsync<WheelPlannerFullSnapshot>(
                resourceStream, JsonOptions, cancellationToken);

            if (snapshot is null)
            {
                logger.LogWarning("Embedded wheel planner snapshot could not be deserialized.");
                return new WheelPlannerFullSnapshot(WheelPlannerLayoutSnapshot.Empty, [], []);
            }

            logger.LogInformation(
                "Loaded embedded wheel planner snapshot: {SectionCount} sections, {SlotCount} slots, {GemCount} gems, {ModCount} mods.",
                snapshot.Layout.Sections.Count,
                snapshot.Layout.RevelationSlots.Count,
                snapshot.Gems.Count,
                snapshot.Mods.Count);

            return snapshot;
        }
    }
}
