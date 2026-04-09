using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Services.DataBaseService.Assets;
using TibiaDataApi.Services.DataBaseService.Assets.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/assets")]
    public sealed class AssetsController(IAssetStreamService streamService) : PublicApiControllerBase
    {
        [HttpGet("{id:int}")]
        [EndpointSummary("Get asset image")]
        [EndpointDescription("Get asset image by id")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAssetImage([FromRoute]int id, CancellationToken cancellationToken = default)
        {
            if(id == 0)
            {
                return BadRequest("Please provide a valid asset id.");
            }

            AssetStreamResult? assetData = await streamService.OpenReadAsync(id, cancellationToken);

            if(assetData is null)
            {
                return NotFound("Asset not found.");
            }

            return File(
                assetData.Stream,
                assetData.MimeType ?? "application/octet-stream",
                assetData.FileName,
                true);
        }
    }
}