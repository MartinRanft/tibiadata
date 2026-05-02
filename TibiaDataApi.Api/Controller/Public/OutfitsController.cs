using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Outfits;
using TibiaDataApi.Services.DataBaseService.Outfits.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/outfits")]
    public class OutfitsController(IOutfitsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all outfits")]
        [EndpointDescription("Get a list of all outfit articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<OutfitListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<OutfitListItemResponse>>> GetOutfitList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<OutfitListItemResponse> outfits = await service.GetOutfitsAsync(cancellationToken);
            return Ok(outfits);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get outfit details by name")]
        [EndpointDescription("Get detailed information about an outfit by its name.")]
        [ProducesResponseType(typeof(OutfitDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OutfitDetailsResponse>> GetOutfitDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Outfit name cannot be null or empty.");
            }

            OutfitDetailsResponse? outfit = await service.GetOutfitDetailsByNameAsync(name, cancellationToken);
            return outfit is null ? NotFound("Outfit not found.") : Ok(outfit);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get outfit details by id")]
        [EndpointDescription("Get detailed information about an outfit by its id.")]
        [ProducesResponseType(typeof(OutfitDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OutfitDetailsResponse>> GetOutfitDetailsById([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Outfit id must be greater than 0.");
            }

            OutfitDetailsResponse? outfit = await service.GetOutfitDetailsByIdAsync(id, cancellationToken);
            return outfit is null ? NotFound("Outfit not found.") : Ok(outfit);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get outfit sync states")]
        [EndpointDescription("Get outfit ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetOutfitSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetOutfitSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get outfit sync states by date")]
        [EndpointDescription("Get outfit ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetOutfitSyncStatesByDate([FromQuery]DateTime? time, CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetOutfitSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}