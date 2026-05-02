using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Items;
using TibiaDataApi.Services.DataBaseService.Items.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/items")]
    public sealed class ItemsController(IItemsDataBaseService itemsDataBaseService) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all item names")]
        [EndpointDescription("Retrieves a complete list of all item names available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<string>>> GetItemNameList(CancellationToken cancellationToken)
        {
            List<string> items = await itemsDataBaseService.GetItemNamesAsync(cancellationToken);
            return Ok(items);
        }

        [HttpGet("categories")]
        [EndpointSummary("Get all item categories")]
        [EndpointDescription("Retrieves a complete list of all item categories available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<string>>> GetItemCategoryList(CancellationToken cancellationToken)
        {
            List<string> categories = await itemsDataBaseService.GetItemCategoriesAsync(cancellationToken);
            return Ok(categories);
        }

        [HttpGet]
        [EndpointSummary("Get paged items")]
        [EndpointDescription("Retrieves a paged list of items. Optional filters: itemName, category, primaryType, secondaryType, objectClass, vocation. Supported sort values: name, category, primary-type, secondary-type, object-class, vocation, last-updated.")]
        [ProducesResponseType(typeof(PagedResponse<ItemListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<ItemListItemResponse>>> GetDetailedItemList(
            [FromQuery]int page = 1,
            [FromQuery]int pageSize = 100,
            [FromQuery]string? itemName = null,
            [FromQuery]string? category = null,
            [FromQuery]string? primaryType = null,
            [FromQuery]string? secondaryType = null,
            [FromQuery]string? objectClass = null,
            [FromQuery]string? vocation = null,
            [FromQuery]string? sort = null,
            [FromQuery]bool descending = false,
            CancellationToken cancellationToken = default)
        {
            if(page < 1)
            {
                return BadRequest("Page must be greater than 0.");
            }

            if(pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100.");
            }

            if(!IsValidItemSort(sort))
            {
                return BadRequest("Sort must be one of: name, category, primary-type, secondary-type, object-class, vocation, last-updated.");
            }

            PagedResponse<ItemListItemResponse> items = await itemsDataBaseService.GetItemsAsync(
                page,
                pageSize,
                itemName,
                category,
                primaryType,
                secondaryType,
                objectClass,
                vocation,
                sort,
                descending,
                cancellationToken);
            return Ok(items);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get Item by Name")]
        [EndpointDescription("Get a detailed item by name.")]
        [ProducesResponseType(typeof(ItemDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ItemDetailsResponse?>> GetItemByName(
            [FromRoute]string? name = null,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name cannot be null or whitespace.");
            }

            ItemDetailsResponse? item = await itemsDataBaseService.GetItemByNameAsync(name, cancellationToken);

            if(item is null)
            {
                return NotFound("Item not found.");
            }

            return Ok(item);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get Item by ID")]
        [EndpointDescription("Get Item Details by ID")]
        [ProducesResponseType(typeof(ItemDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ItemDetailsResponse?>> GetItemById(
            [FromRoute]int? id = null,
            CancellationToken cancellationToken = default)
        {
            if(id is null)
            {
                return BadRequest("Id cannot be null.");
            }

            ItemDetailsResponse? item = await itemsDataBaseService.GetItemByIdAsync(id, cancellationToken);

            if(item is null)
            {
                return NotFound("Item not found.");
            }

            return Ok(item);
        }

        [HttpGet("categories/{category}")]
        [EndpointSummary("Get Item by Category")]
        [EndpointDescription("Get all Items from Category")]
        [ProducesResponseType(typeof(List<ItemListItemResponse?>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<ItemListItemResponse?>>> GetItemsByCat(
            [FromRoute]string? category = null,
            [FromQuery]int page = 1,
            [FromQuery]int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(category))
            {
                return BadRequest("Category cannot be null.");
            }

            if(page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page must be 1 and PageSize must be 100 or less.");
            }
            List<ItemListItemResponse> items = await itemsDataBaseService.GetItemsByCategoryAsync(category, page, pageSize, cancellationToken);

            if(items.Count == 0)
            {
                return NotFound("No items found.");
            }

            return Ok(items);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get item sync states")]
        [EndpointDescription("Get item ids with last updated timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetItemUpdates(CancellationToken cancellationToken = default)
        {
            List<SyncStateResponse> updates = await itemsDataBaseService.GetItemUpdates(cancellationToken);
            return Ok(updates);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get item sync states by date")]
        [EndpointDescription("Get item ids with last updated timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetItemUpdatesByDate(
            [FromQuery]DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            List<SyncStateResponse> updates = await itemsDataBaseService.GetItemUpdatesByDate(time.Value, cancellationToken);
            return Ok(updates);
        }

        private static bool IsValidItemSort(string? sort)
        {
            if(string.IsNullOrWhiteSpace(sort))
            {
                return true;
            }

            return sort.Trim().ToLowerInvariant() is
                "name" or
                "category" or
                "primary-type" or
                "secondary-type" or
                "object-class" or
                "vocation" or
                "last-updated";
        }
    }
}
