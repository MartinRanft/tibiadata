using System.Buffers;

namespace TibiaDataApi.Services.Assets
{
    public static class AssetContentInspector
    {
        public static string? DetectMimeType(ReadOnlySpan<byte> content)
        {
            if(content.Length >= 12 &&
               content[0] == (byte)'R' &&
               content[1] == (byte)'I' &&
               content[2] == (byte)'F' &&
               content[3] == (byte)'F' &&
               content[8] == (byte)'W' &&
               content[9] == (byte)'E' &&
               content[10] == (byte)'B' &&
               content[11] == (byte)'P')
            {
                return "image/webp";
            }

            if(content.Length >= 8 &&
               content[0] == 0x89 &&
               content[1] == 0x50 &&
               content[2] == 0x4E &&
               content[3] == 0x47 &&
               content[4] == 0x0D &&
               content[5] == 0x0A &&
               content[6] == 0x1A &&
               content[7] == 0x0A)
            {
                return "image/png";
            }

            if(content.Length >= 6)
            {
                string header = System.Text.Encoding.ASCII.GetString(content[..6]);
                if(header is "GIF87a" or "GIF89a")
                {
                    return "image/gif";
                }
            }

            if(content.Length >= 3 &&
               content[0] == 0xFF &&
               content[1] == 0xD8 &&
               content[2] == 0xFF)
            {
                return "image/jpeg";
            }

            return null;
        }

        public static string? DetectMimeType(Stream stream)
        {
            if(!stream.CanRead)
            {
                return null;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(32);

            try
            {
                long originalPosition = stream.CanSeek ? stream.Position : 0;
                int bytesRead = stream.Read(buffer, 0, 32);

                if(stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }

                return DetectMimeType(buffer.AsSpan(0, bytesRead));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static string ResolveExtension(string? mimeType, string? fallbackName = null)
        {
            string? extension = NormalizeExtensionFromMimeType(mimeType);
            if(!string.IsNullOrWhiteSpace(extension))
            {
                return extension;
            }

            string fallbackExtension = Path.GetExtension(fallbackName ?? string.Empty);
            if(!string.IsNullOrWhiteSpace(fallbackExtension))
            {
                return fallbackExtension.ToLowerInvariant();
            }

            return ".bin";
        }

        public static string NormalizeFileName(string fileName, string? mimeType)
        {
            string extension = ResolveExtension(mimeType, fileName);
            return Path.ChangeExtension(fileName, extension);
        }

        private static string? NormalizeExtensionFromMimeType(string? mimeType)
        {
            return mimeType?.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/jpeg" => ".jpg",
                _ => null
            };
        }
    }
}
