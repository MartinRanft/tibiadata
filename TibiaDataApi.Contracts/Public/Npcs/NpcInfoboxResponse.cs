namespace TibiaDataApi.Contracts.Public.Npcs
{
    public sealed record NpcInfoboxResponse(
        string? Name,
        string? ActualName,
        string? Implemented,
        string? City,
        string? Race,
        string? Job,
        string? Job2,
        string? Gender,
        string? BuySell,
        string? Location,
        string? Subarea,
        string? Sounds,
        string? Notes,
        string? Status,
        string? PositionX,
        string? PositionY,
        string? PositionZ,
        string? PositionX2,
        string? PositionY2,
        string? PositionZ2);
}