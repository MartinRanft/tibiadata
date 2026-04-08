using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Assets
{
    public interface ICreatureImageAssetService
    {
        Task SyncPrimaryImageAsync(
            TibiaDbContext db,
            Creature creature,
            string wikiPageTitle,
            CancellationToken cancellationToken = default);
    }
}