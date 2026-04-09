namespace TibiaDataApi.Services.Categories
{
    public sealed record WikiCategoryDefinition(
        string Slug,
        string Name,
        WikiContentType ContentType,
        string GroupSlug,
        string GroupName,
        WikiCategorySourceKind SourceKind,
        string SourceTitle,
        string? SourceSection,
        string? ObjectClass,
        int SortOrder,
        bool IsActive = true);
}