namespace TibiaDataApi.Contracts.Public.Corpses
{
    public sealed record CorpseInfoboxResponse(
        string? Name,
        string? ActualName,
        string? Implemented,
        string? Article,
        string? CorpseOf,
        string? Liquid,
        string? Skinable,
        string? Product,
        string? SellTo,
        string? Notes,
        string? FlavourText,
        string? DecayTime1,
        string? DecayTime2,
        string? DecayTime3,
        string? Volume1,
        string? Volume2,
        string? Volume3,
        string? Weight1,
        string? Weight2,
        string? Weight3);
}