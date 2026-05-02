using System.Text.Json.Serialization;

namespace TibiaDataApi.Contracts.Public.HuntingPlaces
{
        public sealed class HuntingPlaceLowerLevel
    {
        [JsonPropertyName("areaname")]public string? AreaName { get; set; }

        [JsonPropertyName("lvlknights")]public string? LevelKnights { get; set; }

        [JsonPropertyName("lvlpaladins")]public string? LevelPaladins { get; set; }

        [JsonPropertyName("lvlmages")]public string? LevelMages { get; set; }

        [JsonPropertyName("skknights")]public string? SkillKnights { get; set; }

        [JsonPropertyName("skpaladins")]public string? SkillPaladins { get; set; }

        [JsonPropertyName("skmages")]public string? SkillMages { get; set; }

        [JsonPropertyName("defknights")]public string? DefenseKnights { get; set; }

        [JsonPropertyName("defpaladins")]public string? DefensePaladins { get; set; }

        [JsonPropertyName("defmages")]public string? DefenseMages { get; set; }

        [JsonPropertyName("loot")]public string? Loot { get; set; }

        [JsonPropertyName("exp")]public string? Experience { get; set; }
    }
}