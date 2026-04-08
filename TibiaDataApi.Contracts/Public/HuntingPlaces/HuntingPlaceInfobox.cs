using System.Text.Json.Serialization;

namespace TibiaDataApi.Contracts.Public.HuntingPlaces
{
        public sealed class HuntingPlaceInfobox
    {
        [JsonPropertyName("name")]public string? Name { get; set; }

        [JsonPropertyName("image")]public string? Image { get; set; }

        [JsonPropertyName("implemented")]public string? Implemented { get; set; }

        [JsonPropertyName("city")]public string? City { get; set; }

        [JsonPropertyName("location")]public string? Location { get; set; }

        [JsonPropertyName("vocation")]public string? Vocation { get; set; }

        
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

        [JsonPropertyName("lootstar")]public string? LootStar { get; set; }

        [JsonPropertyName("exp")]public string? Experience { get; set; }

        [JsonPropertyName("expstar")]public string? ExperienceStar { get; set; }

        
        [JsonPropertyName("bestloot")]public string? BestLoot { get; set; }

        [JsonPropertyName("bestloot2")]public string? BestLoot2 { get; set; }

        [JsonPropertyName("bestloot3")]public string? BestLoot3 { get; set; }

        [JsonPropertyName("bestloot4")]public string? BestLoot4 { get; set; }

        [JsonPropertyName("bestloot5")]public string? BestLoot5 { get; set; }

        
        [JsonPropertyName("map")]public string? Map { get; set; }

        [JsonPropertyName("map2")]public string? Map2 { get; set; }

        [JsonPropertyName("map3")]public string? Map3 { get; set; }

        [JsonPropertyName("map4")]public string? Map4 { get; set; }

        
        [JsonPropertyName("lowerlevels")]public string? LowerLevels { get; set; }

        [JsonPropertyName("areaname")]public string? AreaName { get; set; }
    }
}