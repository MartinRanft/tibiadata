using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Scraper.Implementations
{
    public sealed class SwordScraper(
        ITibiaWikiHttpService tibiaWikiHttpService,
        IItemImageSyncService itemImageSyncService,
        ILogger<SwordScraper> logger)
    : CatalogBackedItemScraper("sword-weapons", tibiaWikiHttpService, itemImageSyncService, logger)
    {
    }
}