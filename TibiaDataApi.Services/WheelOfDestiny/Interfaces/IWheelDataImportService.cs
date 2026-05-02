using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.WheelOfDestiny.Interfaces
{
    public interface IWheelDataImportService
    {
        Task<WheelDataImportResult> ImportAsync(
            TibiaDbContext db,
            CancellationToken cancellationToken = default);
    }

    public sealed record WheelDataImportResult(
        int SourceArticleCount,
        int PerksProcessed,
        int Added,
        int Updated,
        int Unchanged,
        int Removed,
        GemImportResult? GemImportResult = null);
}
