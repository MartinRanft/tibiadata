namespace TibiaDataApi.Services.WheelOfDestiny
{
        public sealed record GemImportResult(
        int SourcePageCount,
        int GemsProcessed,
        int ModifiersProcessed,
        int Added,
        int Updated,
        int Unchanged,
        int Removed);
}
