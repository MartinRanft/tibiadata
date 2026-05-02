namespace TibiaDataApi.Contracts.Public.Creatures
{
    public sealed record CreatureResistanceSummaryResponse(
        int? PhysicalPercent,
        int? EarthPercent,
        int? FirePercent,
        int? DeathPercent,
        int? EnergyPercent,
        int? HolyPercent,
        int? IcePercent,
        int? LifeDrainPercent,
        int? DrownPercent,
        int? HealingPercent);
}
