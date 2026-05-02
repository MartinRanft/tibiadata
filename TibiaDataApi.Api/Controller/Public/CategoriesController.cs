using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Categories;
using TibiaDataApi.Services.DataBaseService.Categories.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/categories")]
    public class CategoriesController(ICategoriesDataBaseService service) : PublicApiControllerBase
    {
        [HttpGet("list")]
        [EndpointSummary("Get categories")]
        [EndpointDescription("Get all active public categories.")]
        [ProducesResponseType(typeof(IReadOnlyList<CategoryListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<CategoryListItemResponse>>> GetCategoryList(CancellationToken cancellationToken = default)
        {
            return Ok(await service.GetCategoriesAsync(cancellationToken));
        }

        [HttpGet("group/{group}")]
        [EndpointSummary("Get categories by group")]
        [EndpointDescription("Get all active public categories for a specific group.")]
        [ProducesResponseType(typeof(IReadOnlyList<CategoryListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<CategoryListItemResponse>>> GetCategoryListByGroup(
            [FromRoute]string group,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(group))
            {
                return BadRequest("Group cannot be null or empty.");
            }

            return Ok(await service.GetCategoriesByGroupAsync(group, cancellationToken));
        }

        [HttpGet("{slug}")]
        [EndpointSummary("Get category details by slug")]
        [EndpointDescription("Get category details by slug.")]
        [ProducesResponseType(typeof(CategoryDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryDetailsResponse>> GetCategoryDetailsBySlug(
            [FromRoute]string slug,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest("Slug cannot be null or empty.");
            }

            CategoryDetailsResponse? category = await service.GetCategoryDetailsBySlugAsync(slug, cancellationToken);

            if(category is null)
            {
                return NotFound("Category not found.");
            }

            return Ok(category);
        }

        [HttpGet("{id:int}")]
        [EndpointSummary("Get category details by id")]
        [EndpointDescription("Get category details by id.")]
        [ProducesResponseType(typeof(CategoryDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryDetailsResponse>> GetCategoryDetailsById(
            [FromRoute]int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("You must provide a valid category id.");
            }

            CategoryDetailsResponse? category = await service.GetCategoryDetailsByIdAsync(id, cancellationToken);

            if(category is null)
            {
                return NotFound("Category not found.");
            }

            return Ok(category);
        }
    }
}
