using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Locations;
using TibiaDataApi.Services.DataBaseService.Locations.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/locations")]
    public class LocationsController(ILocationsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all locations")]
        [EndpointDescription("Get a list of all location articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<LocationListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<LocationListItemResponse>>> GetLocationList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LocationListItemResponse> locations = await service.GetLocationsAsync(cancellationToken);
            return Ok(locations);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get location details by name")]
        [EndpointDescription("Get detailed information about a location by its name.")]
        [ProducesResponseType(typeof(LocationDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LocationDetailsResponse>> GetLocationDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Location name cannot be null or empty.");
            }

            LocationDetailsResponse? location = await service.GetLocationDetailsByNameAsync(name, cancellationToken);
            return location is null ? NotFound("Location not found.") : Ok(location);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get location details by id")]
        [EndpointDescription("Get detailed information about a location by its id.")]
        [ProducesResponseType(typeof(LocationDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LocationDetailsResponse>> GetLocationDetailsById([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Location id must be greater than 0.");
            }

            LocationDetailsResponse? location = await service.GetLocationDetailsByIdAsync(id, cancellationToken);
            return location is null ? NotFound("Location not found.") : Ok(location);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get location sync states")]
        [EndpointDescription("Get location ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetLocationSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetLocationSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get location sync states by date")]
        [EndpointDescription("Get location ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetLocationSyncStatesByDate([FromQuery]DateTime? time, CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetLocationSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}