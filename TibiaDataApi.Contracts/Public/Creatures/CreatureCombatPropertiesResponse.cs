namespace TibiaDataApi.Contracts.Public.Creatures
{
public sealed record CreatureCombatPropertiesResponse(
    int? Armor,
    decimal? Mitigation,
    int? MaxDamage,
    int? Speed,
    int? RunsAt,
        bool? IsBoss,
        bool? UsesSpells,
        bool? Pushable,
        bool? PushObjects,
        bool? WalksAround);
}
