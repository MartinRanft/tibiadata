using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Spells;
using TibiaDataApi.Services.DataBaseService.Spells.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/spells")]
    public class SpellsController(ISpellsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all spells")]
        [EndpointDescription("Get a list of all spell articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<SpellListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SpellListItemResponse>>> GetSpellList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SpellListItemResponse> spells = await service.GetSpellsAsync(cancellationToken);
            return Ok(spells);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get spell details by name")]
        [EndpointDescription("Get detailed information about a spell by its name.")]
        [ProducesResponseType(typeof(SpellDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SpellDetailsResponse>> GetSpellDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Spell name cannot be null or empty.");
            }

            SpellDetailsResponse? spell = await service.GetSpellDetailsByNameAsync(name, cancellationToken);
            return spell is null ? NotFound("Spell not found.") : Ok(spell);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get spell details by id")]
        [EndpointDescription("Get detailed information about a spell by its id.")]
        [ProducesResponseType(typeof(SpellDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SpellDetailsResponse>> GetSpellDetailsById([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Spell id must be greater than 0.");
            }

            SpellDetailsResponse? spell = await service.GetSpellDetailsByIdAsync(id, cancellationToken);
            return spell is null ? NotFound("Spell not found.") : Ok(spell);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get spell sync states")]
        [EndpointDescription("Get spell ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetSpellSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetSpellSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get spell sync states by date")]
        [EndpointDescription("Get spell ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetSpellSyncStatesByDate([FromQuery]DateTime? time, CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetSpellSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}