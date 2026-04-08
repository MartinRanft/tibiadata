using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Keys;
using TibiaDataApi.Services.DataBaseService.Keys.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/keys")]
    public class KeysController(IKeysDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all keys")]
        [EndpointDescription("Get a list of all key items available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<KeyListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<KeyListItemResponse>>> GetKeyList(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<KeyListItemResponse> keys = await service.GetKeysAsync(cancellationToken);
            return Ok(keys);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get key details by name")]
        [EndpointDescription("Get detailed information about a key by its name.")]
        [ProducesResponseType(typeof(KeyDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<KeyDetailsResponse>> GetKeyDetailsByName(
            [FromRoute]string name,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Key name cannot be null or empty.");
            }

            KeyDetailsResponse? key = await service.GetKeyDetailsByNameAsync(name, cancellationToken);
            if(key is null)
            {
                return NotFound("Key not found.");
            }

            return Ok(key);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get key details by id")]
        [EndpointDescription("Get detailed information about a key by its id.")]
        [ProducesResponseType(typeof(KeyDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<KeyDetailsResponse>> GetKeyDetailsById(
            [FromRoute]int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Key id must be greater than 0.");
            }

            KeyDetailsResponse? key = await service.GetKeyDetailsByIdAsync(id, cancellationToken);
            if(key is null)
            {
                return NotFound("Key not found.");
            }

            return Ok(key);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get key sync states")]
        [EndpointDescription("Get key ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetKeySyncStates(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetKeySyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get key sync states by date")]
        [EndpointDescription("Get key ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetKeySyncStatesByDate(
            [FromQuery]DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetKeySyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}