namespace TibiaDataApi.Contracts.Public.Meta
{
    public sealed record ContentSummaryResponse(
        int ItemCount,
        int WikiArticleCount,
        int CreatureCount,
        int CategoryCount,
        DateTime GeneratedAtUtc,
        IReadOnlyList<ContentCountResponse> Breakdown);
}