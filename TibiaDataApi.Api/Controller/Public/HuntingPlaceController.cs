using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.HuntingPlaces;
using TibiaDataApi.Services.DataBaseService.HuntingPlaces.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/hunting-places")]
    public class HuntingPlaceController(IHuntingPlacesDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all Hunting Places")]
        [EndpointDescription("Get a list of all Hunting Places available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<HuntingPlaceListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<HuntingPlaceListItemResponse>>> GetHuntingPlaceList(CancellationToken cancellationToken = default)
        {
            List<HuntingPlaceListItemResponse> huntingPlaces = await service.GetHuntingPlacesAsync(cancellationToken);
            return Ok(huntingPlaces);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get Details of a Hunting Place")]
        [EndpointDescription("Get detailed information about a Hunting Place.")]
        [ProducesResponseType(typeof(HuntingPlaceDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HuntingPlaceDetailsResponse?>> GetHuntingPlaceDetailsByName(
            [FromRoute]string name,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name cannot be null or empty.");
            }

            HuntingPlaceDetailsResponse? huntingPlace = await service.GetHuntingPlaceDetailsByNameAsync(name, cancellationToken);

            if(huntingPlace is null)
            {
                return NotFound("Hunting place not found.");
            }

            return Ok(huntingPlace);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get Details of a Hunting Place")]
        [EndpointDescription("Get detailed information about a Hunting Place.")]
        [ProducesResponseType(typeof(HuntingPlaceDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HuntingPlaceDetailsResponse?>> GetHuntingPlaceDetailsById(
            [FromRoute]int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("You must provide a valid hunting place id.");
            }

            HuntingPlaceDetailsResponse? huntingPlace = await service.GetHuntingPlaceDetailsByIdAsync(id, cancellationToken);

            if(huntingPlace is null)
            {
                return NotFound("Hunting place not found.");
            }

            return Ok(huntingPlace);
        }

        [HttpGet("{name}/area-recommendation")]
        [EndpointSummary("Get Recommendation for a hunting place area")]
        [EndpointDescription("Get a recommendation for a hunting place area based on the hunting place name.")]
        [ProducesResponseType(typeof(HuntingPlaceAreaRecommendationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HuntingPlaceAreaRecommendationResponse?>> GetHuntingPlaceRecommendation(
            [FromRoute]string name,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Please provide a valid hunting place name.");
            }

            HuntingPlaceAreaRecommendationResponse? areaRecommendation =
            await service.GetHuntingPlaceAreaRecommendationAsync(name, cancellationToken);

            if(areaRecommendation is null)
            {
                return NotFound("Hunting place not found.");
            }

            return Ok(areaRecommendation);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get hunting place sync states")]
        [EndpointDescription("Get hunting place ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetHuntingPlaceUpdates(
            CancellationToken cancellationToken = default)
        {
            List<SyncStateResponse> updates = await service.GetHuntingPlaceUpdates(cancellationToken);
            return Ok(updates);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get hunting place sync states by date")]
        [EndpointDescription("Get hunting place ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetHuntingPlaceUpdatesByDate(
            [FromQuery]DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            List<SyncStateResponse> updates = await service.GetHuntingPlaceUpdatesByDate(time.Value, cancellationToken);
            return Ok(updates);
        }
    }
}