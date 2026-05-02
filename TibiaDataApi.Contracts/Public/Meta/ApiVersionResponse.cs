namespace TibiaDataApi.Contracts.Public.Meta
{
    public sealed record ApiVersionResponse(
        int SchemaVersion,
        string DataVersion,
        DateTime GeneratedAtUtc,
        DateTime? LatestDataUpdateUtc,
        int ItemCount,
        int WikiArticleCount,
        int CreatureCount,
        int CategoryCount,
        int AssetCount);
}
