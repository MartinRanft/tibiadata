using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.WheelOfDestiny.Interfaces
{
        public interface IGemModDataImportService
    {
                Task<GemImportResult> ImportGemsAsync(TibiaDbContext db, CancellationToken cancellationToken = default);
    }
}
