namespace TibiaDataApi.Contracts.Public.Objects
{
    public sealed record TibiaObjectInfoboxResponse(
        string? Name,
        string? ActualName,
        string? Article,
        string? Implemented,
        string? ItemId,
        string? PrimaryType,
        string? SecondaryType,
        string? ObjectClass,
        string? Pickupable,
        string? Immobile,
        string? Walkable,
        string? DroppedBy,
        string? SellTo,
        string? BuyFrom,
        string? NpcPrice,
        string? NpcValue,
        string? Value,
        string? Weight,
        string? Marketable,
        string? Stackable,
        string? Notes,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? Fields);
}
