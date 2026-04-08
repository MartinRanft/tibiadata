namespace TibiaDataApi.Contracts.Public.Streets
{
    public sealed record StreetInfoboxResponse(
        string? Name,
        string? ActualName,
        string? Implemented,
        string? City,
        string? City2,
        string? Floor,
        string? Map,
        string? Style,
        string? Notes);
}