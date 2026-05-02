using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Meta;
using TibiaDataApi.Services.DataBaseService.Meta.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/meta")]
    public sealed class MetaController(IMetaDataBaseService service) : PublicApiControllerBase
    {
        private static readonly HashSet<string> SupportedDeltaResources =
        [
            "achievements",
            "assets",
            "books",
            "buildings",
            "categories",
            "charms",
            "corpses",
            "creatures",
            "effects",
            "hunting-places",
            "items",
            "keys",
            "locations",
            "missiles",
            "mounts",
            "npcs",
            "objects",
            "outfits",
            "quests",
            "spells",
            "streets"
        ];

        [HttpGet("version")]
        [EndpointSummary("Get API schema and data version information")]
        [EndpointDescription("Returns the current public schema version and a data version fingerprint for sync and cache-aware clients.")]
        [ProducesResponseType(typeof(ApiVersionResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiVersionResponse>> GetVersion(CancellationToken cancellationToken = default)
        {
            ApiVersionResponse response = await service.GetApiVersionAsync(cancellationToken);
            return Ok(response);
        }

        [HttpGet("snapshot")]
        [EndpointSummary("Get public snapshot manifest")]
        [EndpointDescription("Returns a public snapshot manifest for clients that want to build or refresh a full local mirror of the API.")]
        [ProducesResponseType(typeof(ApiSnapshotResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiSnapshotResponse>> GetSnapshot(CancellationToken cancellationToken = default)
        {
            ApiSnapshotResponse response = await service.GetApiSnapshotAsync(cancellationToken);
            return Ok(response);
        }

        [HttpGet("delta")]
        [EndpointSummary("Get central delta feed")]
        [EndpointDescription("Returns a pollable delta feed for clients that want centralized change notifications across public resources. The since parameter is required and must be a UTC timestamp.")]
        [ProducesResponseType(typeof(ApiDeltaFeedResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiDeltaFeedResponse>> GetDelta(
            [FromQuery]DateTime? since,
            [FromQuery]string? resources = null,
            [FromQuery]int limit = 250,
            CancellationToken cancellationToken = default)
        {
            if(since is null)
            {
                return BadRequest("Please provide a valid since query parameter.");
            }

            if(limit < 1 || limit > 500)
            {
                return BadRequest("Limit must be between 1 and 500.");
            }

            List<string> parsedResources = ParseResources(resources);
            List<string> unknownResources = parsedResources
                                            .Where(x => !SupportedDeltaResources.Contains(x))
                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                            .OrderBy(x => x, StringComparer.Ordinal)
                                            .ToList();

            if(unknownResources.Count > 0)
            {
                return BadRequest($"Unknown delta resources: {string.Join(", ", unknownResources)}.");
            }

            ApiDeltaFeedResponse response = await service.GetApiDeltaFeedAsync(
                since.Value.ToUniversalTime(),
                parsedResources,
                limit,
                cancellationToken);

            return Ok(response);
        }

        private static List<string> ParseResources(string? resources)
        {
            if(string.IsNullOrWhiteSpace(resources))
            {
                return [];
            }

            return resources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(x => x.ToLowerInvariant())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
        }
    }
}
