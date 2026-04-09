using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Charms;
using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Services.DataBaseService.Charms.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/charms")]
    public class CharmsController(ICharmsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all charms")]
        [EndpointDescription("Get a list of all charms available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<CharmListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CharmListItemResponse>>> GetCharmList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CharmListItemResponse> charms = await service.GetCharmsAsync(cancellationToken);
            return Ok(charms);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get charm sync states")]
        [EndpointDescription("Get charm ids with last updated timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetCharmSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetCharmSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get the details of a charm")]
        [EndpointDescription("Get detailed information about a charm.")]
        [ProducesResponseType(typeof(CharmDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CharmDetailsResponse?>> GetCharmDetailsByName(
            [FromRoute]string name,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Charm name cannot be null or empty.");
            }

            CharmDetailsResponse? charm = await service.GetCharmDetailsByNameAsync(name, cancellationToken);

            if(charm is null)
            {
                return NotFound("Charm not found.");
            }

            return Ok(charm);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get the details of a charm")]
        [EndpointDescription("Get detailed information about a charm.")]
        [ProducesResponseType(typeof(CharmDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CharmDetailsResponse?>> GetCharmDetailsById(
            [FromRoute]int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Charm id must be greater than 0.");
            }

            CharmDetailsResponse? charm = await service.GetCharmDetailsByIdAsync(id, cancellationToken);

            if(charm is null)
            {
                return NotFound("Charm not found.");
            }

            return Ok(charm);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get charm sync states by date")]
        [EndpointDescription("Get charm ids with last updated timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetCharmSyncStatesByDate(
            [FromQuery]DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetCharmSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}