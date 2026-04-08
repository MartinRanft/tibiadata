namespace TibiaDataApi.Contracts.Public.WikiPages
{
    public sealed record WikiPageInfoboxResponse(
        string? Name,
        string? ActualName,
        string? Article,
        string? Implemented,
        string? PrimaryType,
        string? ObjectClass,
        string? ItemId,
        string? Pickupable,
        string? Immobile,
        string? Walkable,
        string? DroppedBy,
        string? SellTo,
        string? BuyFrom,
        string? NpcValue,
        string? NpcPrice,
        string? Value,
        string? Weight,
        string? Plural,
        string? Location,
        string? Notes);
}