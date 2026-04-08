using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Missiles;
using TibiaDataApi.Services.DataBaseService.Missiles.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/missiles")]
    public class MissilesController(IMissilesDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all missiles")]
        [EndpointDescription("Get a list of all missile articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<MissileListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<MissileListItemResponse>>> GetMissileList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MissileListItemResponse> missiles = await service.GetMissilesAsync(cancellationToken);
            return Ok(missiles);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get missile details by name")]
        [EndpointDescription("Get detailed information about a missile by its name.")]
        [ProducesResponseType(typeof(MissileDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MissileDetailsResponse>> GetMissileDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Missile name cannot be null or empty.");
            }

            MissileDetailsResponse? missile = await service.GetMissileDetailsByNameAsync(name, cancellationToken);
            return missile is null ? NotFound("Missile not found.") : Ok(missile);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get missile details by id")]
        [EndpointDescription("Get detailed information about a missile by its id.")]
        [ProducesResponseType(typeof(MissileDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MissileDetailsResponse>> GetMissileDetailsById([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Missile id must be greater than 0.");
            }

            MissileDetailsResponse? missile = await service.GetMissileDetailsByIdAsync(id, cancellationToken);
            return missile is null ? NotFound("Missile not found.") : Ok(missile);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get missile sync states")]
        [EndpointDescription("Get missile ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetMissileSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetMissileSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get missile sync states by date")]
        [EndpointDescription("Get missile ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetMissileSyncStatesByDate([FromQuery]DateTime? time, CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetMissileSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}