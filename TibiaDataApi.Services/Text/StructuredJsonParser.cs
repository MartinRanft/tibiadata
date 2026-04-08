using System.Text.Json;

namespace TibiaDataApi.Services.Text
{
    public static class StructuredJsonParser
    {
        public static JsonElement? ParseJsonElement(string? json)
        {
            if(string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static IReadOnlyDictionary<string, string>? ParseStringDictionary(string? json)
        {
            if(string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}