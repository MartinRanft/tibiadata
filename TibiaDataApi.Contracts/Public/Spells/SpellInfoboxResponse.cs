namespace TibiaDataApi.Contracts.Public.Spells
{
    public sealed record SpellInfoboxResponse(
        string? Name,
        string? Premium,
        string? Subclass,
        string? Voc,
        string? Mana,
        string? Soul,
        string? Type,
        string? SpellId,
        string? LevelRequired,
        string? Cooldown,
        string? CooldownGroup,
        string? CooldownGroup2,
        string? Words,
        string? Effect,
        string? DamageType,
        string? Animation,
        string? BasePower,
        string? Implemented,
        string? Notes,
        string? History);
}