using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Bosstiary;
using TibiaDataApi.Services.DataBaseService.Bosstiary.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/bosstiary")]
    public class BosstiaryController(IBosstiaryDataBaseService service) : PublicApiControllerBase
    {
        private const string PublicSortValuesText = "name, category, total-points, total-kills, last-updated";

        private static readonly HashSet<string> AllowedPointOrderValues =
        [
            "name",
            "category",
            "points",
            "total-points",
            "totalpoints",
            "kills",
            "total-kills",
            "totalkills",
            "total-kills-required",
            "totalkillsrequired",
            "updated",
            "last-updated",
            "lastupdated"
        ];

        [HttpGet("categories")]
        [EndpointSummary("Get bosstiary categories")]
        [EndpointDescription("Get all static bosstiary categories with their current creature counts.")]
        [ProducesResponseType(typeof(IReadOnlyList<BosstiaryCategoryResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<BosstiaryCategoryResponse>>> GetBosstiaryCategories(
            CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetBosstiaryCategoriesAsync(cancellationToken));
        }

        [HttpGet("categories/{category}/creatures")]
        [EndpointSummary("Get bosstiary creatures by category")]
        [EndpointDescription("Get all boss creatures that belong to a specific bosstiary category.")]
        [ProducesResponseType(typeof(BosstiaryCategoryCreaturesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BosstiaryCategoryCreaturesResponse>> GetBosstiaryCreaturesByCategory(
            [FromRoute(Name = "category")] string category,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(category))
            {
                return BadRequest("Bosstiary category cannot be null or empty.");
            }

            BosstiaryCategoryCreaturesResponse? result = await service.GetBosstiaryCreaturesByCategoryAsync(category, cancellationToken);

            if(result is null)
            {
                return NotFound("Bosstiary category not found.");
            }

            return Ok(result);
        }

        [HttpGet("points")]
        [EndpointSummary("Get bosstiary point overview")]
        [EndpointDescription("Get all bosstiary creatures with their total point rewards and kill requirements. Use the optional sort query parameter. Allowed values: name, category, total-points, total-kills, last-updated.")]
        [ProducesResponseType(typeof(IReadOnlyList<BosstiaryPointOverviewItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<BosstiaryPointOverviewItemResponse>>> GetBosstiaryPointOverview(
            [FromQuery(Name = "sort")]
            [Description("Optional sort field. Allowed values: name, category, total-points, total-kills, last-updated.")]
            string? sort = "total-points",
            [FromQuery]
            [Description("Optional. Set to true to return the result in descending order.")]
            bool descending = false,
            CancellationToken cancellationToken = default)
        {
            string normalizedSort = string.IsNullOrWhiteSpace(sort)
                ? "total-points"
                : sort.Trim().ToLowerInvariant();

            if(!AllowedPointOrderValues.Contains(normalizedSort))
            {
                return BadRequest($"Invalid sort value. Allowed values: {PublicSortValuesText}.");
            }

            return Ok(await service.GetBosstiaryPointOverviewAsync(normalizedSort, descending, cancellationToken));
        }

        [HttpGet("creatures")]
        [EndpointSummary("Get filtered bosstiary creatures")]
        [EndpointDescription("Get bosstiary creatures using the optional filters category, totalPoints, creatureName and sort. Allowed sort values: name, category, total-points, total-kills, last-updated.")]
        [ProducesResponseType(typeof(BosstiaryFilteredCreaturesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BosstiaryFilteredCreaturesResponse>> GetFilteredBosstiaryCreatures(
            [FromQuery]
            [Description("Optional filter for the bosstiary category, for example Bane, Archfoe or Nemesis.")]
            string? category = null,
            [FromQuery(Name = "totalPoints")]
            [Description("Optional filter for the exact total point reward.")]
            [Range(0, int.MaxValue)]
            int? totalPoints = null,
            [FromQuery(Name = "creatureName")]
            [Description("Optional creature name search.")]
            string? creatureName = null,
            [FromQuery(Name = "sort")]
            [Description("Optional sort field. Allowed values: name, category, total-points, total-kills, last-updated.")]
            string? sort = "name",
            [FromQuery]
            [Description("Optional. Set to true to return the result in descending order.")]
            bool descending = false,
            [FromQuery]
            [Description("Optional page number. Starts at 1.")]
            [Range(1, int.MaxValue)]
            int page = 1,
            [FromQuery]
            [Description("Optional page size. Allowed range: 1 to 250.")]
            [Range(1, 250)]
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            string? resolvedCategory = ResolveQueryValue(category, "category");
            string? resolvedCreatureName = ResolveQueryValue(creatureName, "creatureName", "search");
            string? requestedSort = ResolveQueryValue(sort, "sort", "orderBy");
            string normalizedSort = string.IsNullOrWhiteSpace(requestedSort)
                ? "name"
                : requestedSort.Trim().ToLowerInvariant();

            if(totalPoints is < 0)
            {
                return BadRequest("Total points cannot be negative.");
            }

            if(!AllowedPointOrderValues.Contains(normalizedSort))
            {
                return BadRequest($"Invalid sort value. Allowed values: {PublicSortValuesText}.");
            }

            if(!string.IsNullOrWhiteSpace(resolvedCategory))
            {
                IReadOnlyList<BosstiaryCategoryResponse> categories = await service.GetBosstiaryCategoriesAsync(cancellationToken);

                if(!categories.Any(entry => MatchesLookup(entry.Name, entry.Slug, resolvedCategory)))
                {
                    return BadRequest("Unknown bosstiary category.");
                }
            }

            return Ok(await service.GetFilteredBosstiaryCreaturesAsync(
                resolvedCategory,
                totalPoints,
                resolvedCreatureName,
                normalizedSort,
                descending,
                page,
                pageSize,
                cancellationToken));
        }

        private string? ResolveQueryValue(string? currentValue, params string[] queryKeys)
        {
            foreach(string queryKey in queryKeys)
            {
                if(Request.Query.TryGetValue(queryKey, out Microsoft.Extensions.Primitives.StringValues values))
                {
                    string? resolved = values.FirstOrDefault();

                    if(!string.IsNullOrWhiteSpace(resolved))
                    {
                        return resolved;
                    }
                }
            }

            return currentValue;
        }

        private static bool MatchesLookup(string name, string slug, string candidate)
        {
            string normalizedCandidate = NormalizeLookupValue(candidate);
            return string.Equals(NormalizeLookupValue(name), normalizedCandidate, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(NormalizeLookupValue(slug), normalizedCandidate, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeLookupValue(string value)
        {
            return value.Trim()
                        .ToLowerInvariant()
                        .Replace('_', '-')
                        .Replace(' ', '-');
        }
    }
}
