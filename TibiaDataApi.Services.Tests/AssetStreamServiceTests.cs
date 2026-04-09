using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.DataBaseService.Assets;
using TibiaDataApi.Services.DataBaseService.Assets.Interfaces;

namespace TibiaDataApi.Services.Tests
{
    public sealed class AssetStreamServiceTests
    {
        [Fact]
        public async Task OpenReadAsync_DetectsActualMimeTypeAndNormalizesFileName()
        {
            string rootPath = Path.Combine(Path.GetTempPath(), "tibiadata-asset-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(rootPath, "creatures", "431"));

            try
            {
                string storageKey = "creatures/431/primary.gif";
                string fullPath = Path.Combine(rootPath, "creatures", "431", "primary.gif");

                byte[] webpHeader =
                [
                    0x52, 0x49, 0x46, 0x46,
                    0x1A, 0x00, 0x00, 0x00,
                    0x57, 0x45, 0x42, 0x50,
                    0x56, 0x50, 0x38, 0x20
                ];

                await File.WriteAllBytesAsync(fullPath, webpHeader);

                AssetStreamService service = new(
                    new StubAssetsDataBaseService(new AssetStreamDescriptor(storageKey, "primary.gif", "image/gif")),
                    new AssetStorageOptions
                    {
                        StorageRootPath = rootPath
                    });

                AssetStreamResult? result = await service.OpenReadAsync(431);

                Assert.NotNull(result);
                Assert.Equal("image/webp", result!.MimeType);
                Assert.Equal("primary.webp", result.FileName);
            }
            finally
            {
                if(Directory.Exists(rootPath))
                {
                    Directory.Delete(rootPath, true);
                }
            }
        }

        private sealed class StubAssetsDataBaseService(AssetStreamDescriptor? descriptor) : IAssetsDataBaseService
        {
            public Task<AssetStreamDescriptor?> GetAssetAsync(int assetId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(descriptor);
            }
        }
    }
}
