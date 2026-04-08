using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Mounts;
using TibiaDataApi.Services.DataBaseService.Mounts.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/mounts")]
    public class MountsController(IMountsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all mounts")]
        [EndpointDescription("Get a list of all mount articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<MountListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<MountListItemResponse>>> GetMountList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MountListItemResponse> mounts = await service.GetMountsAsync(cancellationToken);
            return Ok(mounts);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get mount details by name")]
        [EndpointDescription("Get detailed information about a mount by its name.")]
        [ProducesResponseType(typeof(MountDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MountDetailsResponse>> GetMountDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Mount name cannot be null or empty.");
            }

            MountDetailsResponse? mount = await service.GetMountDetailsByNameAsync(name, cancellationToken);
            return mount is null ? NotFound("Mount not found.") : Ok(mount);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get mount details by id")]
        [EndpointDescription("Get detailed information about a mount by its id.")]
        [ProducesResponseType(typeof(MountDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MountDetailsResponse>> GetMountDetailsById([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Mount id must be greater than 0.");
            }

            MountDetailsResponse? mount = await service.GetMountDetailsByIdAsync(id, cancellationToken);
            return mount is null ? NotFound("Mount not found.") : Ok(mount);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get mount sync states")]
        [EndpointDescription("Get mount ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetMountSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetMountSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get mount sync states by date")]
        [EndpointDescription("Get mount ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetMountSyncStatesByDate([FromQuery]DateTime? time, CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetMountSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}