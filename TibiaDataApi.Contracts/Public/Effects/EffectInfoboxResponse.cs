namespace TibiaDataApi.Contracts.Public.Effects
{
    public sealed record EffectInfoboxResponse(
        string? Name,
        string? Implemented,
        string? PrimaryType,
        string? SecondaryType,
        string? Causes,
        string? EffectId,
        string? Effect,
        string? LightColor,
        string? LightRadius,
        string? Notes,
        string? History,
        string? Status);
}