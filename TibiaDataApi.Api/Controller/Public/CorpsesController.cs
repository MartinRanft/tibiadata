using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Corpses;
using TibiaDataApi.Services.DataBaseService.Corpses.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/corpses")]
    public class CorpsesController(ICorpsesDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all corpses")]
        [EndpointDescription("Get a list of all corpse articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<CorpseListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<CorpseListItemResponse?>>> GetCorpseList(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CorpseListItemResponse?> corpses = await service.GetCorpseNamesAsync(cancellationToken);
            return Ok(corpses);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get corpse details by name")]
        [EndpointDescription("Get detailed information about a corpse by its name.")]
        [ProducesResponseType(typeof(CorpseDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CorpseDetailsResponse>> GetCorpseDetailsByName(
            [FromRoute]string name,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Corpse name cannot be null or empty.");
            }

            CorpseDetailsResponse? corpse = await service.GetCorpseDetailsByNameAsync(name, cancellationToken);
            if(corpse is null)
            {
                return NotFound("Corpse not found.");
            }

            return Ok(corpse);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get corpse details by id")]
        [EndpointDescription("Get detailed information about a corpse by its id.")]
        [ProducesResponseType(typeof(CorpseDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CorpseDetailsResponse>> GetCorpseDetailsById(
            [FromRoute]int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Corpse id must be greater than 0.");
            }

            CorpseDetailsResponse? corpse = await service.GetCorpseDetailsByIdAsync(id, cancellationToken);
            if(corpse is null)
            {
                return NotFound("Corpse not found.");
            }

            return Ok(corpse);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get corpse sync states")]
        [EndpointDescription("Get corpse ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetCorpseSyncStates(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetCorpseSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get corpse sync states by date")]
        [EndpointDescription("Get corpse ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetCorpseSyncStatesByDate(
            [FromQuery]DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetCorpseSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}