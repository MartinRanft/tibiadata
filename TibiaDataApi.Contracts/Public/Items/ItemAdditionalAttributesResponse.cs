namespace TibiaDataApi.Contracts.Public.Items
{
    public sealed record ItemAdditionalAttributesResponse(
        IReadOnlyList<ItemAttributeEntryResponse> Entries);
}