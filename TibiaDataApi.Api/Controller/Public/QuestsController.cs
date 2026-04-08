using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Quests;
using TibiaDataApi.Services.DataBaseService.Quests.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/quests")]
    public class QuestsController(IQuestsDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all quests")]
        [EndpointDescription("Get a list of all quest articles available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<QuestListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<QuestListItemResponse>>> GetQuestList(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<QuestListItemResponse> quests = await service.GetQuestsAsync(cancellationToken);
            return Ok(quests);
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get quest details by name")]
        [EndpointDescription("Get detailed information about a quest by its name.")]
        [ProducesResponseType(typeof(QuestDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QuestDetailsResponse>> GetQuestDetailsByName([FromRoute]string name, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Quest name cannot be null or empty.");
            }

            QuestDetailsResponse? quest = await service.GetQuestDetailsByNameAsync(name, cancellationToken);
            return quest is null ? NotFound("Quest not found.") : Ok(quest);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get quest details by id")]
        [EndpointDescription("Get detailed information about a quest by its id.")]
        [ProducesResponseType(typeof(QuestDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QuestDetailsResponse>> GetQuestDetailsById([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Quest id must be greater than 0.");
            }

            QuestDetailsResponse? quest = await service.GetQuestDetailsByIdAsync(id, cancellationToken);
            return quest is null ? NotFound("Quest not found.") : Ok(quest);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get quest sync states")]
        [EndpointDescription("Get quest ids with last updated and last seen timestamps for sync clients.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetQuestSyncStates(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetQuestSyncStatesAsync(cancellationToken);
            return Ok(syncStates ?? []);
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get quest sync states by date")]
        [EndpointDescription("Get quest ids with last updated and last seen timestamps starting from the provided UTC date.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SyncStateResponse>>> GetQuestSyncStatesByDate([FromQuery]DateTime? time, CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Please provide a valid time query parameter.");
            }

            IReadOnlyList<SyncStateResponse>? syncStates = await service.GetQuestSyncStatesByDateTimeAsync(time.Value, cancellationToken);
            return Ok(syncStates ?? []);
        }
    }
}