using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Entities.Assets;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.TibiaWiki;

namespace TibiaDataApi.Services.Assets
{
    public sealed class ItemImageAssetService(
        ITibiaWikiHttpService tibiaWikiHttpService,
        AssetStorageOptions assetStorageOptions,
        IHostEnvironment hostEnvironment,
        ILogger<ItemImageAssetService> logger) : IItemImageAssetService
    {
        private readonly AssetStorageOptions _assetStorageOptions = assetStorageOptions;
        private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
        private readonly ILogger<ItemImageAssetService> _logger = logger;
        private readonly ITibiaWikiHttpService _tibiaWikiHttpService = tibiaWikiHttpService;

        public async Task SyncPrimaryImageAsync(
            TibiaDbContext db,
            Item item,
            string wikiPageTitle,
            CancellationToken cancellationToken = default)
        {
            if(!_assetStorageOptions.DownloadItemImages || item.Id <= 0)
            {
                return;
            }

            WikiImageInfo? wikiImage = await ResolvePrimaryImageAsync(wikiPageTitle, item.Name, cancellationToken);
            ItemAsset? existingRelation = await db.ItemAssets
                                                  .Include(entry => entry.Asset)
                                                  .FirstOrDefaultAsync(
                                                      entry => entry.ItemId == item.Id &&
                                                               entry.AssetKind == AssetKind.PrimaryImage &&
                                                               entry.IsPrimary,
                                                      cancellationToken);

            if(wikiImage is null)
            {
                await RemovePrimaryImageAsync(db, existingRelation, cancellationToken);
                return;
            }

            byte[] content = await DownloadAssetContentAsync(wikiImage.Url, cancellationToken);
            string contentHashSha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            string mimeType = AssetContentInspector.DetectMimeType(content) ?? wikiImage.MimeType ?? "application/octet-stream";
            string extension = ResolveExtension(mimeType, wikiImage.FileTitle);
            string fileName = $"primary{extension}";
            string storageKey = $"{_assetStorageOptions.ItemImageDirectory.Trim('/').Trim()}/{item.Id}/{fileName}";
            string fullPath = ResolveAbsolutePath(storageKey);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            Asset asset;
            string? existingContentHash = existingRelation?.Asset?.ContentSha256;
            if(existingRelation?.Asset is null)
            {
                asset = new Asset
                {
                    StorageKey = storageKey,
                    FileName = fileName,
                    SourcePageTitle = wikiPageTitle,
                    SourceFileTitle = wikiImage.FileTitle,
                    SourceUrl = wikiImage.Url,
                    MimeType = mimeType,
                    Extension = extension,
                    SizeBytes = content.LongLength,
                    Width = wikiImage.Width,
                    Height = wikiImage.Height,
                    SourceSha1 = wikiImage.SourceSha1,
                    ContentSha256 = contentHashSha256,
                    DownloadedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.Assets.Add(asset);
                db.ItemAssets.Add(new ItemAsset
                {
                    ItemId = item.Id,
                    Item = item,
                    Asset = asset,
                    AssetKind = AssetKind.PrimaryImage,
                    SortOrder = 0,
                    IsPrimary = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                asset = existingRelation.Asset;

                if(!string.Equals(asset.StorageKey, storageKey, StringComparison.OrdinalIgnoreCase))
                {
                    DeleteFileIfExists(asset.StorageKey);
                }

                existingRelation.UpdatedAt = DateTime.UtcNow;
                asset.StorageKey = storageKey;
                asset.FileName = fileName;
                asset.SourcePageTitle = wikiPageTitle;
                asset.SourceFileTitle = wikiImage.FileTitle;
                asset.SourceUrl = wikiImage.Url;
                asset.MimeType = mimeType;
                asset.Extension = extension;
                asset.SizeBytes = content.LongLength;
                asset.Width = wikiImage.Width;
                asset.Height = wikiImage.Height;
                asset.SourceSha1 = wikiImage.SourceSha1;
                asset.ContentSha256 = contentHashSha256;
                asset.DownloadedAt = DateTime.UtcNow;
                asset.UpdatedAt = DateTime.UtcNow;
            }

            if(!File.Exists(fullPath) ||
               !string.Equals(existingContentHash, contentHashSha256, StringComparison.OrdinalIgnoreCase))
            {
                await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
            }

            _logger.LogDebug(
                "Synchronized primary image for item {ItemName} to {StorageKey}.",
                item.Name,
                storageKey);
        }

        private async Task RemovePrimaryImageAsync(
            TibiaDbContext db,
            ItemAsset? existingRelation,
            CancellationToken cancellationToken)
        {
            if(existingRelation?.Asset is null)
            {
                return;
            }

            string storageKey = existingRelation.Asset.StorageKey;
            Asset asset = existingRelation.Asset;

            db.ItemAssets.Remove(existingRelation);

            bool hasOtherReferences = await db.ItemAssets
                                              .AnyAsync(entry => entry.AssetId == asset.Id && entry.Id != existingRelation.Id, cancellationToken);

            if(!hasOtherReferences)
            {
                DeleteFileIfExists(storageKey);
                db.Assets.Remove(asset);
            }
        }

        private async Task<WikiImageInfo?> ResolvePrimaryImageAsync(
            string wikiPageTitle,
            string itemName,
            CancellationToken cancellationToken)
        {
            string imagesUrl =
            $"api.php?action=query&titles={Uri.EscapeDataString(wikiPageTitle)}&prop=images&imlimit=max&format=json";

            string imagesJson = await _tibiaWikiHttpService.GetStringAsync(imagesUrl, cancellationToken).ConfigureAwait(false);
            JsonNode? imagesNode = JsonNode.Parse(imagesJson);
            JsonObject? pages = imagesNode?["query"]?["pages"]?.AsObject();

            if(pages is null || pages.Count == 0)
            {
                return null;
            }

            IReadOnlyList<string> imageTitles = pages
                                                .SelectMany(page => page.Value?["images"]?.AsArray() ?? [])
                                                .Select(image => image?["title"]?.ToString())
                                                .Where(title => !string.IsNullOrWhiteSpace(title))
                                                .Select(title => title!)
                                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                                .ToList();

            if(imageTitles.Count == 0)
            {
                return null;
            }

            string selectedImageTitle = SelectPrimaryImageTitle(imageTitles, wikiPageTitle, itemName);
            return await GetImageInfoAsync(selectedImageTitle, cancellationToken);
        }

        private async Task<WikiImageInfo?> GetImageInfoAsync(string imageTitle, CancellationToken cancellationToken)
        {
            string imageInfoUrl =
            $"api.php?action=query&titles={Uri.EscapeDataString(imageTitle)}&prop=imageinfo&iiprop=url|size|mime|sha1&format=json";

            string imageInfoJson = await _tibiaWikiHttpService.GetStringAsync(imageInfoUrl, cancellationToken).ConfigureAwait(false);
            JsonNode? imageInfoNode = JsonNode.Parse(imageInfoJson);
            JsonNode? imageInfo = imageInfoNode?["query"]?["pages"]?.AsObject()
                                                                   .Select(page => page.Value?["imageinfo"]?[0])
                                                                   .FirstOrDefault(entry => entry is not null);

            string? url = imageInfo?["url"]?.ToString();
            if(string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            return new WikiImageInfo(
                imageTitle,
                url,
                imageInfo?["mime"]?.ToString(),
                imageInfo?["sha1"]?.ToString(),
                TryReadInt(imageInfo?["width"]),
                TryReadInt(imageInfo?["height"]));
        }

        private async Task<byte[]> DownloadAssetContentAsync(string assetUrl, CancellationToken cancellationToken)
        {
            return await _tibiaWikiHttpService.GetBytesAsync(assetUrl, cancellationToken).ConfigureAwait(false);
        }

        private string ResolveAbsolutePath(string storageKey)
        {
            string normalizedRoot = _assetStorageOptions.StorageRootPath.Replace('\\', Path.DirectorySeparatorChar);
            string rootPath = Path.IsPathRooted(normalizedRoot)
            ? normalizedRoot
            : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, normalizedRoot));

            string relativePath = storageKey.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(rootPath, relativePath);
        }

        private void DeleteFileIfExists(string storageKey)
        {
            string fullPath = ResolveAbsolutePath(storageKey);

            if(!File.Exists(fullPath))
            {
                return;
            }

            File.Delete(fullPath);
        }

        private static string SelectPrimaryImageTitle(
            IReadOnlyList<string> imageTitles,
            string wikiPageTitle,
            string itemName)
        {
            string normalizedPageTitle = NormalizeTitle(wikiPageTitle);
            string normalizedItemName = NormalizeTitle(itemName);

            string? exactMatch = imageTitles.FirstOrDefault(title =>
            {
                string normalizedFileBaseName = NormalizeFileBaseName(title);
                return string.Equals(normalizedFileBaseName, normalizedPageTitle, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(normalizedFileBaseName, normalizedItemName, StringComparison.OrdinalIgnoreCase);
            });

            return exactMatch ?? imageTitles[0];
        }

        private static string ResolveExtension(string mimeType, string fileTitle)
        {
            return AssetContentInspector.ResolveExtension(mimeType, fileTitle);
        }

        private static string NormalizeFileBaseName(string fileTitle)
        {
            string withoutPrefix = fileTitle.StartsWith("File:", StringComparison.OrdinalIgnoreCase)
            ? fileTitle["File:".Length..]
            : fileTitle;

            return NormalizeTitle(Path.GetFileNameWithoutExtension(withoutPrefix));
        }

        private static string NormalizeTitle(string value)
        {
            return value.Replace('_', ' ').Trim();
        }

        private static int? TryReadInt(JsonNode? node)
        {
            return node is null ? null : node.Deserialize<int?>();
        }

        private sealed record WikiImageInfo(
            string FileTitle,
            string Url,
            string? MimeType,
            string? SourceSha1,
            int? Width,
            int? Height);
    }
}
