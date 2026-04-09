using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Effects;
using TibiaDataApi.Services.DataBaseService.Effects.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/effects")]
    public class EffectsController(IEffectsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all effects")]
        [EndpointDescription("Get a list of all effect articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<EffectListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<EffectListItemResponse>?>> GetEffectList(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<EffectListItemResponse>? effects = await service.GetEffectNamesAsync(cancellationToken);
            return Ok(effects ?? []);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get effect details by name")]
        [EndpointDescription("Get detailed information about an effect by its name.")]
        [ProducesResponseType(typeof(EffectDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EffectDetailsResponse>> GetEffectDetailsByName(
            [FromRoute]string name,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Effect name cannot be null or empty.");
            }

            EffectDetailsResponse? effect = await service.GetEffectDetailsByNameAsync(name, cancellationToken);
            if(effect is null)
            {
                return NotFound("Effect not found.");
            }

            return Ok(effect);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get effect details by id")]
        [EndpointDescription("Get detailed information about an effect by its id.")]
        [ProducesResponseType(typeof(EffectDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EffectDetailsResponse>> GetEffectDetailsById(
            [FromRoute]int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Effect id must be greater than 0.");
            }

            EffectDetailsResponse? effect = await service.GetEffectDetailsByIdAsync(id, cancellationToken);
            if(effect is null)
            {
                return NotFound("Effect not found.");
            }

            return Ok(effect);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get effect sync states")]
        [EndpointDescription("Get effect ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetEffectSyncStates(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetEffectSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get effect sync states by date")]
        [EndpointDescription("Get effect ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetEffectSyncStatesByDate(
            [FromQuery]DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetEffectSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}