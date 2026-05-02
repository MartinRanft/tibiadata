using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.WheelOfDestiny
{
        public sealed record WheelPlannerFullSnapshot(
        WheelPlannerLayoutSnapshot Layout,
        List<WheelPlannerGemSnapshot> Gems,
        List<WheelPlannerModSnapshot> Mods);
}
