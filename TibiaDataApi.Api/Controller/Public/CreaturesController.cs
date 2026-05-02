using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Creatures;
using TibiaDataApi.Contracts.Public.LootStatistics;
using TibiaDataApi.Services.DataBaseService.Creatures.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/creatures")]
    public class CreaturesController(ICreaturesDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet]
        [EndpointSummary("Get paged creatures")]
        [EndpointDescription("Retrieves a paged list of creatures. Optional filters: creatureName, minHitpoints, maxHitpoints, minExperience, maxExperience. Supported sort values: name, hitpoints, experience, last-updated.")]
        [ProducesResponseType(typeof(PagedResponse<CreatureListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<CreatureListItemResponse>>> GetCreatureList(
            [FromQuery]int page = 1,
            [FromQuery]int pageSize = 100,
            [FromQuery]string? creatureName = null,
            [FromQuery]int? minHitpoints = null,
            [FromQuery]int? maxHitpoints = null,
            [FromQuery]long? minExperience = null,
            [FromQuery]long? maxExperience = null,
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

            if(minHitpoints is < 0 || maxHitpoints is < 0)
            {
                return BadRequest("Hitpoints filters cannot be negative.");
            }

            if(minExperience is < 0 || maxExperience is < 0)
            {
                return BadRequest("Experience filters cannot be negative.");
            }

            if(minHitpoints.HasValue && maxHitpoints.HasValue && minHitpoints.Value > maxHitpoints.Value)
            {
                return BadRequest("minHitpoints cannot be greater than maxHitpoints.");
            }

            if(minExperience.HasValue && maxExperience.HasValue && minExperience.Value > maxExperience.Value)
            {
                return BadRequest("minExperience cannot be greater than maxExperience.");
            }

            if(!IsValidCreatureSort(sort))
            {
                return BadRequest("Sort must be one of: name, hitpoints, experience, last-updated.");
            }

            PagedResponse<CreatureListItemResponse> creatures = await service.GetCreatureListAsync(
                page,
                pageSize,
                creatureName,
                minHitpoints,
                maxHitpoints,
                minExperience,
                maxExperience,
                sort,
                descending,
                cancellationToken);

            return Ok(creatures);
        }

        [HttpGet("list")]
        [EndpointSummary("Get all creature names")]
        [EndpointDescription("Get a list of all creature names available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<string>>> GetCreatureNameList(CancellationToken cancellationToken = default)
        {
            List<string> creatures = await service.GetCreaturesAsync(cancellationToken);
            return Ok(creatures);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get creature sync states")]
        [EndpointDescription("Get creature ids with last updated timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetCreatureSyncStates(CancellationToken cancellationToken = default)
        {
            List<SyncStateResponse> syncStates = await service.GetCreatureSyncStatesAsync(cancellationToken);
            return Ok(syncStates);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get the details of a creature")]
        [EndpointDescription("Get detailed information about a creature.")]
        [ProducesResponseType(typeof(CreatureDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreatureDetailsResponse>> GetCreatureDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(name))
            {
                return BadRequest("Name cannot be null or empty.");
            }

            CreatureDetailsResponse? creature = await service.GetCreatureDetailsByNameAsync(name, cancellationToken);

            if(creature is null)
            {
                return NotFound("Creature not found.");
            }

            return Ok(creature);
        }

        [HttpGet("{name}/loot")]
        [EndpointSummary("Get loot statistics for a creature")]
        [EndpointDescription("Get only the structured loot statistics for a creature by name.")]
        [ProducesResponseType(typeof(LootStatisticDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LootStatisticDetailsResponse>> GetCreatureLootByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name cannot be null or empty.");
            }

            LootStatisticDetailsResponse? loot = await service.GetCreatureLootByNameAsync(name, cancellationToken);

            if(loot is null)
            {
                return NotFound("Creature loot not found.");
            }

            return Ok(loot);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get the details of a creature")]
        [EndpointDescription("Get detailed information about a creature.")]
        [ProducesResponseType(typeof(CreatureDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreatureDetailsResponse>> GetCreatureDetailsById([FromRoute]int id = 0, CancellationToken cancellationToken = default)
        {
            if(id == 0)
            {
                return BadRequest("You must provide a valid creature id.");
            }

            CreatureDetailsResponse? creature = await service.GetCreatureDetailsByIdAsync(id, cancellationToken);

            if(creature is null)
            {
                return NotFound("Creature not found.");
            }

            return Ok(creature);
        }

        [HttpGet("{id:int}/loot")]
        [EndpointSummary("Get loot statistics for a creature")]
        [EndpointDescription("Get only the structured loot statistics for a creature by id.")]
        [ProducesResponseType(typeof(LootStatisticDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LootStatisticDetailsResponse>> GetCreatureLootById([FromRoute]int id = 0, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("You must provide a valid creature id.");
            }

            LootStatisticDetailsResponse? loot = await service.GetCreatureLootByIdAsync(id, cancellationToken);

            if(loot is null)
            {
                return NotFound("Creature loot not found.");
            }

            return Ok(loot);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get creature sync states by date")]
        [EndpointDescription("Get creature ids with last updated timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetCreatureSyncStatesByDate(
            [FromQuery]DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            List<SyncStateResponse> syncStates = await service.GetCreatureSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates);
        }

        private static bool IsValidCreatureSort(string? sort)
        {
            if(string.IsNullOrWhiteSpace(sort))
            {
                return true;
            }

            return sort.Trim().ToLowerInvariant() is
                "name" or
                "hitpoints" or
                "experience" or
                "last-updated";
        }
    }
}
