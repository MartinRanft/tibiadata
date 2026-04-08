namespace TibiaDataApi.Contracts.Public.Missiles
{
    public sealed record MissileInfoboxResponse(
        string? Name,
        string? Implemented,
        string? PrimaryType,
        string? SecondaryType,
        string? ShotBy,
        string? MissileId,
        string? LightRadius,
        string? LightColor,
        string? Notes);
}