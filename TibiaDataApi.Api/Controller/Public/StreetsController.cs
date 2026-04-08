using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Streets;
using TibiaDataApi.Services.DataBaseService.Streets.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/streets")]
    public class StreetsController(IStreetsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all streets")]
        [EndpointDescription("Get a list of all street articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<StreetListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<StreetListItemResponse>>> GetStreetList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<StreetListItemResponse> streets = await service.GetStreetsAsync(cancellationToken);
            return Ok(streets);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get street details by name")]
        [EndpointDescription("Get detailed information about a street by its name.")]
        [ProducesResponseType(typeof(StreetDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StreetDetailsResponse>> GetStreetDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Street name cannot be null or empty.");
            }

            StreetDetailsResponse? street = await service.GetStreetDetailsByNameAsync(name, cancellationToken);
            return street is null ? NotFound("Street not found.") : Ok(street);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get street details by id")]
        [EndpointDescription("Get detailed information about a street by its id.")]
        [ProducesResponseType(typeof(StreetDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StreetDetailsResponse>> GetStreetDetailsById([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Street id must be greater than 0.");
            }

            StreetDetailsResponse? street = await service.GetStreetDetailsByIdAsync(id, cancellationToken);
            return street is null ? NotFound("Street not found.") : Ok(street);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get street sync states")]
        [EndpointDescription("Get street ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetStreetSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetStreetSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get street sync states by date")]
        [EndpointDescription("Get street ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetStreetSyncStatesByDate([FromQuery]DateTime? time, CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetStreetSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}