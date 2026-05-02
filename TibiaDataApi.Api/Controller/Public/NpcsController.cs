using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Npcs;
using TibiaDataApi.Services.DataBaseService.Npcs.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/npcs")]
    public class NpcsController(INpcsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all NPCs")]
        [EndpointDescription("Get a list of all NPC articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<NpcListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<NpcListItemResponse>>> GetNpcList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<NpcListItemResponse> npcs = await service.GetNpcsAsync(cancellationToken);
            return Ok(npcs);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get NPC details by name")]
        [EndpointDescription("Get detailed information about an NPC by its name.")]
        [ProducesResponseType(typeof(NpcDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<NpcDetailsResponse>> GetNpcDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("NPC name cannot be null or empty.");
            }

            NpcDetailsResponse? npc = await service.GetNpcDetailsByNameAsync(name, cancellationToken);
            return npc is null ? NotFound("NPC not found.") : Ok(npc);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get NPC details by id")]
        [EndpointDescription("Get detailed information about an NPC by its id.")]
        [ProducesResponseType(typeof(NpcDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<NpcDetailsResponse>> GetNpcDetailsById([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("NPC id must be greater than 0.");
            }

            NpcDetailsResponse? npc = await service.GetNpcDetailsByIdAsync(id, cancellationToken);
            return npc is null ? NotFound("NPC not found.") : Ok(npc);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get NPC sync states")]
        [EndpointDescription("Get NPC ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetNpcSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetNpcSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get NPC sync states by date")]
        [EndpointDescription("Get NPC ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetNpcSyncStatesByDate([FromQuery]DateTime? time, CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetNpcSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}