namespace TibiaDataApi.Contracts.Public.Meta
{
    public sealed record ApiDeltaEntryResponse(
        string Resource,
        int Id,
        string Identifier,
        DateTime UpdatedAtUtc,
        string ChangeType,
        string Route,
        IReadOnlyList<string> RelatedRoutes);
}
