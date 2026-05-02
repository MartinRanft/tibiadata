using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.WheelOfDestiny;
using TibiaDataApi.Services.DataBaseService.WheelOfDestiny.Interfaces;
using TibiaDataApi.Services.Entities.WheelOfDestiny;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/wheel")]
    public sealed class WheelController(IWheelDataBaseService service) : PublicApiControllerBase
    {
        

        [HttpGet("perks/list")]
        [EndpointSummary("Get all perk names grouped by vocation")]
        [EndpointDescription("Retrieves a complete list of all active Wheel of Destiny perk names, grouped by vocation.")]
        [ProducesResponseType(typeof(Dictionary<WheelVocation, List<string>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<Dictionary<WheelVocation, List<string>>>> GetPerkNames(CancellationToken cancellationToken)
        {
            return Ok(await service.GetPerkNamesAsync(cancellationToken));
        }

        [HttpGet("perks")]
        [EndpointSummary("Get paged perks")]
        [EndpointDescription("Retrieves a paged list of Wheel of Destiny perks. Optional filters: vocation, type, search. Supported sort values: name, vocation, type.")]
        [ProducesResponseType(typeof(PagedResponse<WheelOfDestinyPerkListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<WheelOfDestinyPerkListItemResponse>>> GetPerks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] string? vocation = null,
            [FromQuery] string? type = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sort = null,
            [FromQuery] bool descending = false,
            CancellationToken cancellationToken = default)
        {
            if(page < 1)
            {
                return BadRequest("Page must be greater than 0.");
            }
            
            if(pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100.");
            }
            
            return Ok(await service.GetPerksAsync(page, pageSize, vocation, type, search, sort, descending, cancellationToken));
        }

        [HttpGet("perks/{id:int}")]
        [EndpointSummary("Get perk by ID")]
        [EndpointDescription("Retrieves detailed Wheel of Destiny perk information by its internal ID.")]
        [ProducesResponseType(typeof(WheelOfDestinyPerkDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WheelOfDestinyPerkDetailsResponse>> GetPerkById(
            [FromRoute] int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Perk ID must be greater than 0.");
            }

            WheelOfDestinyPerkDetailsResponse? perk = await service.GetPerkDetailsByIdAsync(id, cancellationToken);
            if(perk is null) return NotFound("Perk not found.");
            return Ok(perk);
        }

        [HttpGet("perks/by-key/{key}")]
        [EndpointSummary("Get perk by key")]
        [EndpointDescription("Retrieves detailed Wheel of Destiny perk information by its unique key (e.g. elder-druid:conviction:healing-link).")]
        [ProducesResponseType(typeof(WheelOfDestinyPerkDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WheelOfDestinyPerkDetailsResponse>> GetPerkByKey(
            [FromRoute] string key,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(key))
            {
                return BadRequest("Perk key cannot be null or empty.");
            }
            
            WheelOfDestinyPerkDetailsResponse? perk = await service.GetPerkDetailsByKeyAsync(key, cancellationToken);
            if(perk is null) return NotFound("Perk not found.");
            return Ok(perk);
        }

        [HttpGet("perks/{vocation}/{slug}")]
        [EndpointSummary("Get perk by vocation and slug")]
        [EndpointDescription("Retrieves detailed Wheel of Destiny perk information by vocation and slug.")]
        [ProducesResponseType(typeof(WheelOfDestinyPerkDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WheelOfDestinyPerkDetailsResponse>> GetPerkBySlug(
            [FromRoute] string vocation,
            [FromRoute] string slug,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(vocation) || string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest("Vocation and slug cannot be null or empty.");                                                                                                                                                                                                                
            }
            
            WheelOfDestinyPerkDetailsResponse? perk = await service.GetPerkDetailsBySlugAsync(slug, vocation, cancellationToken);
            if(perk is null) return NotFound("Perk not found.");
            return Ok(perk);
        }

        [HttpGet("perks/sync")]
        [EndpointSummary("Get perk sync states")]
        [EndpointDescription("Retrieves all perk IDs with last updated timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetPerkSyncStates(CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetPerkSyncStatesAsync(cancellationToken));
        }

        [HttpGet("perks/sync/by-date")]
        [EndpointSummary("Get perk sync states by date")]
        [EndpointDescription("Retrieves perk IDs with last updated timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetPerkSyncStatesByDate(
            [FromQuery] DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");                                                                                                                                                                                                               
            }
            
            return Ok(await service.GetPerkSyncStatesByDateTimeAsync(time.Value, cancellationToken));                                                                                                                                                                                                               
        }

        

        [HttpGet("layout/{vocation}")]
        [EndpointSummary("Get wheel layout by vocation")]
        [EndpointDescription("Retrieves the full Wheel of Destiny layout for a vocation, including sections, dedication links, and revelation slots.")]
        [ProducesResponseType(typeof(WheelOfDestinyLayoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WheelOfDestinyLayoutResponse>> GetLayout(
            [FromRoute] string vocation,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(vocation))
            {
                return BadRequest("Vocation cannot be null or empty.");
            }
            
            WheelOfDestinyLayoutResponse? layout = await service.GetLayoutByVocationAsync(vocation, cancellationToken);
            if(layout is null) return NotFound("Layout not found.");
            return Ok(layout);
        }

        

        [HttpGet("gems")]
        [EndpointSummary("Get gems")]
        [EndpointDescription("Retrieves all Wheel of Destiny gems. Optional filter: vocation.")]
        [ProducesResponseType(typeof(List<WheelOfDestinyGemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<WheelOfDestinyGemResponse>>> GetGems(
            [FromQuery] string? vocation = null,
            CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetGemsAsync(vocation, cancellationToken));
        }

        [HttpGet("gems/sync")]
        [EndpointSummary("Get gem sync states")]
        [EndpointDescription("Retrieves all gem IDs with last updated timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetGemSyncStates(CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetGemSyncStatesAsync(cancellationToken));                                                                                                                                                                                                               
        }

        [HttpGet("gems/sync/by-date")]
        [EndpointSummary("Get gem sync states by date")]
        [EndpointDescription("Retrieves gem IDs with last updated timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetGemSyncStatesByDate(
            [FromQuery] DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");                                                                                                                                                                                                               
            }
            return Ok(await service.GetGemSyncStatesByDateTimeAsync(time.Value, cancellationToken));                                                                                                                                                                                              
        }

        

        [HttpGet("gem-modifiers")]
        [EndpointSummary("Get paged gem modifiers")]
        [EndpointDescription("Retrieves a paged list of Wheel of Destiny gem modifiers. Optional filters: modifierType, category, vocation, search, hasTradeoff, isComboMod. Supported sort values: name, vocation, type, category.")]
        [ProducesResponseType(typeof(PagedResponse<WheelOfDestinyGemModifierResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<WheelOfDestinyGemModifierResponse>>> GetGemModifiers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] string? modifierType = null,
            [FromQuery] string? category = null,
            [FromQuery] string? vocation = null,
            [FromQuery] string? search = null,
            [FromQuery] bool? hasTradeoff = null,
            [FromQuery] bool? isComboMod = null,
            [FromQuery] string? sort = null,
            [FromQuery] bool descending = false,
            CancellationToken cancellationToken = default)
        {
            if(page < 1)
            {
                return BadRequest("Page must be greater than 0.");
            }
            
            if(pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100.");
            }
            
            return Ok(await service.GetGemModifiersAsync(page, pageSize, modifierType, category, vocation, search, hasTradeoff, isComboMod, sort, descending, cancellationToken));                                                                                                                                                                                                               
        }

        [HttpGet("gem-modifiers/sync")]
        [EndpointSummary("Get gem modifier sync states")]
        [EndpointDescription("Retrieves all gem modifier IDs with last updated timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetGemModifierSyncStates(CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetGemModifierSyncStatesAsync(cancellationToken));                                                                                                                                                                                                              
        }

        [HttpGet("gem-modifiers/sync/by-date")]
        [EndpointSummary("Get gem modifier sync states by date")]
        [EndpointDescription("Retrieves gem modifier IDs with last updated timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetGemModifierSyncStatesByDate(
            [FromQuery] DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }
            return Ok(await service.GetGemModifierSyncStatesByDateTimeAsync(time.Value, cancellationToken));                                                                                                                                                                                                              
        }
    }
}