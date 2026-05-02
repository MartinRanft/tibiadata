namespace TibiaDataApi.Contracts.Public.Mounts
{
    public sealed record MountInfoboxResponse(
        string? Name,
        string? ActualName,
        string? MountId,
        string? TamingMethod,
        string? Implemented,
        string? Speed,
        string? Bought,
        string? Price,
        string? Achievement,
        string? Tournament,
        string? Colourisable,
        string? Artwork,
        string? Notes,
        string? History,
        string? LightColor,
        string? LightRadius,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? Fields);
}
