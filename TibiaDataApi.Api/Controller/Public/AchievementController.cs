using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Achievements;
using TibiaDataApi.Services.DataBaseService.Achievements.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/achievements")]
    public class AchievementController(IAchievementsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all achievements")]
        [EndpointDescription("Get a list of all achievements available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<AchievementListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AchievementListItemResponse>>> GetAchievementList(CancellationToken cancellationToken = default)
        {
            List<AchievementListItemResponse> achievements = await service.GetAchievementsAsync(cancellationToken);
            return Ok(achievements);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get the details of an achievement")]
        [EndpointDescription("Get detailed information about an achievement.")]
        [ProducesResponseType(typeof(AchievementDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AchievementDetailsResponse?>> GetAchievementDetailsByName(
            [FromRoute]string name,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Achievement name cannot be null or empty.");
            }

            AchievementDetailsResponse? achievement = await service.GetAchievementDetailsByNameAsync(name, cancellationToken);

            if(achievement is null)
            {
                return NotFound("Achievement not found.");
            }

            return Ok(achievement);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get the details of an achievement")]
        [EndpointDescription("Get detailed information about an achievement.")]
        [ProducesResponseType(typeof(AchievementDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AchievementDetailsResponse?>> GetAchievementDetailsById(
            [FromRoute]int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Achievement id must be greater than 0.");
            }

            AchievementDetailsResponse? achievement = await service.GetAchievementDetailsByIdAsync(id, cancellationToken);

            if(achievement is null)
            {
                return NotFound("Achievement not found.");
            }

            return Ok(achievement);
        }
    }
}