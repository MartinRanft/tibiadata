using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Services.WheelOfDestiny
{
        public sealed record WheelPlannerGemSnapshot(
        string Name,
        GemFamily Family,
        GemSize Size,
        GemVocation? VocationRestriction,
        List<string> PossibleModNames);
}
