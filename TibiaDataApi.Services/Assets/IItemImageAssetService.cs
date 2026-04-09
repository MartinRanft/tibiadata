using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Assets
{
    public interface IItemImageAssetService
    {
        Task SyncPrimaryImageAsync(
            TibiaDbContext db,
            Item item,
            string wikiPageTitle,
            CancellationToken cancellationToken = default);
    }
}