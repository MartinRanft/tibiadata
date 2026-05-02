using Microsoft.AspNetCore.Mvc;

using TibiaDataApi.Contracts.Public.Assets;
using TibiaDataApi.Services.DataBaseService.Assets;
using TibiaDataApi.Services.DataBaseService.Assets.Interfaces;

namespace TibiaDataApi.Controller.Public
{
    [ApiController]
    [Route("api/v1/assets")]
    public sealed class AssetsController(
        IAssetStreamService streamService,
        IAssetsDataBaseService service) : PublicApiControllerBase
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

        [HttpGet("metadata/{id:int}")]
        [EndpointSummary("Get asset metadata")]
        [EndpointDescription("Get Information about an asset by id")]
        [ProducesResponseType(typeof(AssetMetadataResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AssetMetadataResponse?>> GetMetaData(
            [FromRoute]int id,
            CancellationToken cancellationToken = default)
        {
            if(id <= 0)
            {
                return BadRequest("Please provide a valid asset id.");
            }

            AssetMetadataResponse? metadata = await service.GetAssetMetaDataAsync(id, cancellationToken);

            if(metadata is null)
            {
                return NotFound("Asset not found.");
            }

            return Ok(metadata);
        }

        [HttpGet("metadata/search")]
        [EndpointSummary("Search asset metadata by file name")]
        [EndpointDescription("Search asset metadata by file name without downloading the binary.")]
        [ProducesResponseType(typeof(IReadOnlyList<AssetMetadataResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<AssetMetadataResponse>>> SearchMetaData(
            [FromQuery]string fileName,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("Please provide a valid fileName query parameter.");
            }

            IReadOnlyList<AssetMetadataResponse> metadata = await service.SearchAssetMetadataByFileNameAsync(fileName, cancellationToken);
            return Ok(metadata);
        }
    }
}
