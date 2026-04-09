namespace TibiaDataApi.Contracts.Public.Locations
{
    public sealed record LocationInfoboxResponse(
        string? Name,
        string? Implemented,
        string? Ruler,
        string? Population,
        string? Organization,
        string? Organizations,
        string? Links,
        string? Near,
        string? Status,
        string? SeeAlso,
        string? Image,
        string? Map,
        string? Map2,
        string? Map3,
        string? Map4,
        string? Map6,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? Fields);
}
