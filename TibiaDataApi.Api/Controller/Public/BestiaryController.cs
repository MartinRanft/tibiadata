using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Bestiary;
using TibiaDataApi.Services.DataBaseService.Bestiary.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/bestiary")]
    public class BestiaryController(IBestiaryDataBaseService service) : PublicApiControllerBase
    {
        private const string PublicSortValuesText = "name, bestiary-class, category, difficulty, charm-points, total-kills, last-updated";

        private static readonly HashSet<string> AllowedCharmPointOrderValues =
        [
            "name",
            "class",
            "bestiary-class",
            "bestiaryclass",
            "category",
            "difficulty",
            "points",
            "charm-points",
            "charmpoints",
            "kills",
            "total-kills",
            "totalkills",
            "total-kills-required",
            "totalkillsrequired",
            "updated",
            "last-updated",
            "lastupdated"
        ];

        private static readonly HashSet<string> AllowedCreatureOrderValues =
        [
            "name",
            "class",
            "bestiary-class",
            "bestiaryclass",
            "category",
            "difficulty",
            "points",
            "charm-points",
            "charmpoints",
            "kills",
            "total-kills",
            "totalkills",
            "total-kills-required",
            "totalkillsrequired",
            "updated",
            "last-updated",
            "lastupdated"
        ];

        [HttpGet("classes")]
        [EndpointSummary("Get bestiary classes")]
        [EndpointDescription("Get all static bestiary classes with their current creature counts.")]
        [ProducesResponseType(typeof(IReadOnlyList<BestiaryClassResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<BestiaryClassResponse>>> GetBestiaryClasses(
            CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetBestiaryClassesAsync(cancellationToken));
        }

        [HttpGet("categories")]
        [EndpointSummary("Get bestiary categories")]
        [EndpointDescription("Get all static bestiary categories with their current creature counts.")]
        [ProducesResponseType(typeof(IReadOnlyList<BestiaryCategoryResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<BestiaryCategoryResponse>>> GetBestiaryCategories(
            CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetBestiaryCategoriesAsync(cancellationToken));
        }

        [HttpGet("categories/{category}/creatures")]
        [EndpointSummary("Get bestiary creatures by category")]
        [EndpointDescription("Get all creatures that belong to a specific bestiary category.")]
        [ProducesResponseType(typeof(BestiaryCategoryCreaturesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BestiaryCategoryCreaturesResponse>> GetBestiaryCreaturesByCategory(
            [FromRoute(Name = "category")] string category,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(category))
            {
                return BadRequest("Bestiary category cannot be null or empty.");
            }

            BestiaryCategoryCreaturesResponse? result = await service.GetBestiaryCreaturesByCategoryAsync(category, cancellationToken);

            if(result is null)
            {
                return NotFound("Bestiary category not found.");
            }

            return Ok(result);
        }

        [HttpGet("difficulties")]
        [EndpointSummary("Get bestiary difficulties")]
        [EndpointDescription("Get all bestiary difficulty levels with their static kill requirements and charm points.")]
        [ProducesResponseType(typeof(IReadOnlyList<BestiaryDifficultyResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<BestiaryDifficultyResponse>>> GetBestiaryDifficulties(
            CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetBestiaryDifficultiesAsync(cancellationToken));
        }

        [HttpGet("difficulties/{difficulty}/creatures")]
        [EndpointSummary("Get bestiary creatures by difficulty")]
        [EndpointDescription("Get all creatures that belong to a specific bestiary difficulty.")]
        [ProducesResponseType(typeof(BestiaryDifficultyCreaturesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BestiaryDifficultyCreaturesResponse>> GetBestiaryCreaturesByDifficulty(
            [FromRoute(Name = "difficulty")] string difficulty,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(difficulty))
            {
                return BadRequest("Bestiary difficulty cannot be null or empty.");
            }

            BestiaryDifficultyCreaturesResponse? result = await service.GetBestiaryCreaturesByDifficultyAsync(difficulty, cancellationToken);

            if(result is null)
            {
                return NotFound("Bestiary difficulty not found.");
            }

            return Ok(result);
        }

        [HttpGet("charm-points")]
        [EndpointSummary("Get bestiary charm point overview")]
        [EndpointDescription("Get all bestiary creatures with their charm point rewards and total kill requirements. Use the optional sort query parameter. Allowed values: name, bestiary-class, category, difficulty, charm-points, total-kills, last-updated.")]
        [ProducesResponseType(typeof(IReadOnlyList<BestiaryCharmPointOverviewItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<BestiaryCharmPointOverviewItemResponse>>> GetBestiaryCharmPointOverview(
            [FromQuery(Name = "sort")]
            [Description("Optional sort field. Allowed values: name, bestiary-class, category, difficulty, charm-points, total-kills, last-updated.")]
            string? sort = "charm-points",
            [FromQuery]
            [Description("Optional. Set to true to return the result in descending order.")]
            bool descending = false,
            CancellationToken cancellationToken = default)
        {
            string? requestedSort = ResolveQueryValue(sort, "sort", "orderBy");
            string normalizedSort = string.IsNullOrWhiteSpace(requestedSort)
                ? "charm-points"
                : requestedSort.Trim().ToLowerInvariant();

            if(!AllowedCharmPointOrderValues.Contains(normalizedSort))
            {
                return BadRequest($"Invalid sort value. Allowed values: {PublicSortValuesText}.");
            }

            return Ok(await service.GetBestiaryCharmPointOverviewAsync(normalizedSort, descending, cancellationToken));
        }

        [HttpGet("creatures")]
        [EndpointSummary("Get filtered bestiary creatures")]
        [EndpointDescription("Get bestiary creatures using the optional filters bestiaryClass, category, difficulty, charmPointReward, creatureName and sort. Allowed sort values: name, bestiary-class, category, difficulty, charm-points, total-kills, last-updated.")]
        [ProducesResponseType(typeof(BestiaryFilteredCreaturesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BestiaryFilteredCreaturesResponse>> GetFilteredBestiaryCreatures(
            [FromQuery(Name = "bestiaryClass")]
            [Description("Optional filter for the bestiary class, for example Humanoid or humanoid.")]
            string? bestiaryClass = null,
            [FromQuery]
            [Description("Optional filter for the bestiary category, for example Humanoid or Vermin.")]
            string? category = null,
            [FromQuery]
            [Description("Optional filter for the bestiary difficulty, for example Harmless, Trivial, Easy, Medium or Hard.")]
            string? difficulty = null,
            [FromQuery(Name = "charmPointReward")]
            [Description("Optional filter for the exact charm point reward.")]
            [Range(0, int.MaxValue)]
            int? charmPointReward = null,
            [FromQuery(Name = "creatureName")]
            [Description("Optional creature name search.")]
            string? creatureName = null,
            [FromQuery(Name = "sort")]
            [Description("Optional sort field. Allowed values: name, bestiary-class, category, difficulty, charm-points, total-kills, last-updated.")]
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
            string? resolvedBestiaryClass = ResolveQueryValue(bestiaryClass, "bestiaryClass", "creatureClass");
            string? resolvedCategory = ResolveQueryValue(category, "category");
            string? resolvedDifficulty = ResolveQueryValue(difficulty, "difficulty");
            string? resolvedCreatureName = ResolveQueryValue(creatureName, "creatureName", "search");
            string? requestedSort = ResolveQueryValue(sort, "sort", "orderBy");
            string normalizedSort = string.IsNullOrWhiteSpace(requestedSort)
                ? "name"
                : requestedSort.Trim().ToLowerInvariant();

            if(charmPointReward is < 0)
            {
                return BadRequest("Charm points cannot be negative.");
            }

            if(!AllowedCreatureOrderValues.Contains(normalizedSort))
            {
                return BadRequest($"Invalid sort value. Allowed values: {PublicSortValuesText}.");
            }

            if(!string.IsNullOrWhiteSpace(resolvedBestiaryClass))
            {
                IReadOnlyList<BestiaryClassResponse> classes = await service.GetBestiaryClassesAsync(cancellationToken);

                if(!classes.Any(entry => MatchesLookup(entry.Name, entry.Slug, resolvedBestiaryClass)))
                {
                    return BadRequest("Unknown bestiary class.");
                }
            }

            if(!string.IsNullOrWhiteSpace(resolvedCategory))
            {
                IReadOnlyList<BestiaryCategoryResponse> categories = await service.GetBestiaryCategoriesAsync(cancellationToken);

                if(!categories.Any(entry => MatchesLookup(entry.Name, entry.Slug, resolvedCategory)))
                {
                    return BadRequest("Unknown bestiary category.");
                }
            }

            if(!string.IsNullOrWhiteSpace(resolvedDifficulty))
            {
                IReadOnlyList<BestiaryDifficultyResponse> difficulties = await service.GetBestiaryDifficultiesAsync(cancellationToken);

                if(!difficulties.Any(entry => MatchesLookup(entry.Name, entry.Slug, resolvedDifficulty)))
                {
                    return BadRequest("Unknown bestiary difficulty.");
                }
            }

            return Ok(await service.GetFilteredBestiaryCreaturesAsync(
                resolvedBestiaryClass,
                resolvedCategory,
                resolvedDifficulty,
                charmPointReward,
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
