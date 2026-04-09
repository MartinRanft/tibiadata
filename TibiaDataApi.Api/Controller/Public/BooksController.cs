using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Books;
using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Services.DataBaseService.Books.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/books")]
    public class BooksController(IBooksDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get all available books")]
        [EndpointDescription("Returns a list of all books available from TibiaWiki.")]
        [ProducesResponseType(typeof(List<BookListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<BookListItemResponse>>> GetBookList(CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetBooksAsync(cancellationToken));
        }

        [HttpGet("{name}")]
        [EndpointSummary("Get book details by name")]
        [EndpointDescription("Returns the full details of a book for the specified name.")]
        [ProducesResponseType(typeof(BookDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookDetailsResponse?>> GetBookDetailsByName(
            [FromRoute]string? name = null,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name cannot be null or empty.");
            }

            BookDetailsResponse? result = await service.GetBookDetailsByNameAsync(name, cancellationToken);

            if(result is null)
            {
                return NotFound("Book not found.");
            }

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get book details by ID")]
        [EndpointDescription("Returns the full details of a book for the specified ID.")]
        [ProducesResponseType(typeof(BookDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookDetailsResponse?>> GetBookDetailsById(
            [FromRoute]int? id = null,
            CancellationToken cancellationToken = default)
        {
            if(id is null or 0)
            {
                return BadRequest("ID cannot be null or 0.");
            }

            int idInt = id.Value;

            BookDetailsResponse? result = await service.GetBookDetailsByIdAsync(idInt, cancellationToken);

            if(result is null)
            {
                return NotFound("Book not found.");
            }

            return Ok(result);
        }

        [HttpGet("sync")]
        [EndpointSummary("Get book sync states")]
        [EndpointDescription("Returns the sync state for all books, including ID, last updated, and last seen timestamps.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetBookSyncStates(
            CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetBookSyncStatesAsync(cancellationToken));
        }

        [HttpGet("sync/by-date")]
        [EndpointSummary("Get book sync states by date")]
        [EndpointDescription("Returns the sync states of all books updated for the specified date or timestamp filter.")]
        [ProducesResponseType(typeof(List<SyncStateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<SyncStateResponse>>> GetBookSyncStatesByDate(
            [FromQuery]DateTime? time,
            CancellationToken cancellationToken = default)
        {
            if(time is null)
            {
                return BadRequest("Date cannot be null.");
            }

            return Ok(await service.GetBookSyncStatesByDateTimeAsync(time.Value, cancellationToken));
        }
    }
}