using System.ComponentModel;

using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Search;
using TibiaDataApi.Services.DataBaseService.Search.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/search")]
    public sealed class SearchController(ISearchDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet]
        [EndpointSummary("Search across common public resources")]
        [EndpointDescription("Searches common public resources such as creatures, items, books, hunting places, NPCs, spells, and more by name or title.")]
        [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SearchResponse>> Search(
            [FromQuery]
            [Description("Required search text. Minimum 2 characters.")]
            string? query = null,
            [FromQuery]
            [Description("Optional comma-separated resource types such as creatures,items,books,npcs.")]
            string? types = null,
            [FromQuery]
            [Description("Maximum number of hits to return. Default 20, maximum 50.")]
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query cannot be null or empty.");
            }

            string trimmedQuery = query.Trim();
            if(trimmedQuery.Length < 2)
            {
                return BadRequest("Query must contain at least 2 characters.");
            }

            if(limit < 1 || limit > 50)
            {
                return BadRequest("Limit must be between 1 and 50.");
            }

            List<string>? requestedTypes = ParseTypes(types);
            IReadOnlyList<string> supportedTypes = service.GetSupportedTypes();
            List<string> invalidTypes = requestedTypes?.Where(x => !supportedTypes.Contains(x, StringComparer.OrdinalIgnoreCase))
                                                     .Distinct(StringComparer.OrdinalIgnoreCase)
                                                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                                     .ToList()
                                      ?? [];

            if(invalidTypes.Count > 0)
            {
                return BadRequest(
                    $"Unknown search types: {string.Join(", ", invalidTypes)}. Supported types: {string.Join(", ", supportedTypes)}.");
            }

            SearchResponse response = await service.SearchAsync(trimmedQuery, requestedTypes, limit, cancellationToken);
            return Ok(response);
        }

        private static List<string>? ParseTypes(string? types)
        {
            if(string.IsNullOrWhiteSpace(types))
            {
                return null;
            }

            return types.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
        }
    }
}
