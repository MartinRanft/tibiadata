using System.Text.Json.Serialization;

namespace TibiaDataApi.Contracts.Public.HuntingPlaces
{
        public sealed class HuntingPlaceAdditionalAttributes
    {
        [JsonPropertyName("LowerLevels")]public List<HuntingPlaceLowerLevel>? LowerLevels { get; set; }
    }
}