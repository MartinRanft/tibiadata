namespace TibiaDataApi.Contracts.Public.Keys
{
    public sealed record KeyInfoboxResponse(
        string? Name,
        string? ActualName,
        string? Number,
        string? Location,
        string? Quest,
        string? Notes,
        string? History,
        string? Status,
        string? Implemented,
        string? Aka,
        string? Origin,
        string? ShortNotes,
        string? LongNotes,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? Fields);
}
