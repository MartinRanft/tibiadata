using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Buildings;
using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Services.DataBaseService.Buildings.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/buildings")]
    public class BuildingsController(IBuildingsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all buildings")]
        [EndpointDescription("Get a list of all buildings available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<BuildingListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<BuildingListItemResponse>>> GetBuildings(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<BuildingListItemResponse> buildings = await service.GetBuildingsAsync(cancellationToken);
            return Ok(buildings);
        }

        [HttpGet("city/{city}")]
        [EndpointSummary("Get buildings by city")]
        [EndpointDescription("Get all buildings that belong to a specific city.")]
        [ProducesResponseType(typeof(List<BuildingListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<BuildingListItemResponse>>> GetBuildingsByCity(
            [FromRoute]string city,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(city))
            {
                return BadRequest("City cannot be null or empty.");
            }

            IReadOnlyList<BuildingListItemResponse>? buildings = await service.GetBuildingsByCityAsync(city, cancellationToken);

            if(buildings is null || buildings.Count == 0)
            {
                return NotFound("No buildings found for the provided city.");
            }

            return Ok(buildings);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get building details by name")]
        [EndpointDescription("Get detailed information about a building by its name.")]
        [ProducesResponseType(typeof(BuildingDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BuildingDetailsResponse>> GetBuildingDetailsByName(
            [FromRoute]string name,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name cannot be null or empty.");
            }

            BuildingDetailsResponse? building = await service.GetBuildingDetailsByNameAsync(name, cancellationToken);

            if(building is null)
            {
                return NotFound("Building not found.");
            }

            return Ok(building);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get building details by id")]
        [EndpointDescription("Get detailed information about a building by its id.")]
        [ProducesResponseType(typeof(BuildingDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BuildingDetailsResponse>> GetBuildingDetailsById(
            [FromRoute]int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("You must provide a valid building id.");
            }

            BuildingDetailsResponse? building = await service.GetBuildingDetailsByIdAsync(id, cancellationToken);

            if(building is null)
            {
                return NotFound("Building not found.");
            }

            return Ok(building);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get building sync states")]
        [EndpointDescription("Get building ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetBuildingSyncStates(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetBuildingSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get building sync states by date")]
        [EndpointDescription("Get building ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetBuildingSyncStatesByDate(
            [FromQuery]DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetBuildingSyncStatesSinceAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}