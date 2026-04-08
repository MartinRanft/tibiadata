using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Creatures;
using TibiaDataApi.Services.DataBaseService.Creatures.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/creatures")]
    public class CreaturesController(ICreaturesDataBaseService service) : PublicApiControllerBase
    {
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
    }
}