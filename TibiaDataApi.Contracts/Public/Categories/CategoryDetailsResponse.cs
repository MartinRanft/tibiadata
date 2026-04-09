namespace TibiaDataApi.Contracts.Public.Categories
{
    public sealed record CategoryDetailsResponse(
        int Id,
        string Slug,
        string Name,
        string ContentType,
        string GroupSlug,
        string GroupName,
        string SourceKind,
        string SourceTitle,
        string? SourceSection,
        string? ObjectClass,
        int SortOrder,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int ItemCount,
        int WikiArticleCount);
}