using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Objects;
using TibiaDataApi.Services.DataBaseService.Objects.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/objects")]
    public class ObjectsController(IObjectsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all objects")]
        [EndpointDescription("Get a list of all object articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<TibiaObjectListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<TibiaObjectListItemResponse>>> GetObjectList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TibiaObjectListItemResponse> objects = await service.GetObjectsAsync(cancellationToken);
            return Ok(objects);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get object details by name")]
        [EndpointDescription("Get detailed information about an object by its name.")]
        [ProducesResponseType(typeof(TibiaObjectDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TibiaObjectDetailsResponse>> GetObjectDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Object name cannot be null or empty.");
            }

            TibiaObjectDetailsResponse? tibiaObject = await service.GetObjectDetailsByNameAsync(name, cancellationToken);
            return tibiaObject is null ? NotFound("Object not found.") : Ok(tibiaObject);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get object details by id")]
        [EndpointDescription("Get detailed information about an object by its id.")]
        [ProducesResponseType(typeof(TibiaObjectDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TibiaObjectDetailsResponse>> GetObjectDetailsById([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Object id must be greater than 0.");
            }

            TibiaObjectDetailsResponse? tibiaObject = await service.GetObjectDetailsByIdAsync(id, cancellationToken);
            return tibiaObject is null ? NotFound("Object not found.") : Ok(tibiaObject);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get object sync states")]
        [EndpointDescription("Get object ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetObjectSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetObjectSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get object sync states by date")]
        [EndpointDescription("Get object ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetObjectSyncStatesByDate([FromQuery]DateTime? time, CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetObjectSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}