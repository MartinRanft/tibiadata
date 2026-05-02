namespace TibiaDataApi.Contracts.Public.Charms
{
    public sealed record CharmInfoboxResponse(
        string? Name,
        string? ActualName,
        string? Type,
        string? Cost,
        string? Effect,
        string? Implemented,
        string? Notes,
        string? History,
        string? Status,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? Fields);
}
