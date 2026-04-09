namespace TibiaDataApi.Contracts.Public.Bosstiary
{
    public sealed record BosstiaryFilterOptionsResponse(
        IReadOnlyList<BosstiaryCategoryResponse> Categories);
}
