namespace TibiaDataApi.Services.Scraper.Parsing
{
    public sealed class ItemCategoryParsingProfile
    {
        public required string Key { get; init; }

        public required IReadOnlyDictionary<string, string[]> FieldAliasExtensions { get; init; }

        public required IReadOnlyDictionary<string, string[]> AdditionalAttributeAliases { get; init; }

        public string[] GetFieldAliases(string fieldKey, params string[] defaultAliases)
        {
            IEnumerable<string> aliases = defaultAliases;

            if(FieldAliasExtensions.TryGetValue(fieldKey, out string[]? extensions))
            {
                aliases = aliases.Concat(extensions);
            }

            return aliases
                   .Where(alias => !string.IsNullOrWhiteSpace(alias))
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToArray();
        }

        public IReadOnlyDictionary<string, string[]> MergeAdditionalAttributeAliases(
            IReadOnlyDictionary<string, string[]> commonAliases)
        {
            Dictionary<string, string[]> merged = new(commonAliases, StringComparer.OrdinalIgnoreCase);

            foreach((string key, string[] aliases) in AdditionalAttributeAliases)
            {
                if(!merged.TryGetValue(key, out string[]? existingAliases))
                {
                    merged[key] = aliases
                                  .Where(alias => !string.IsNullOrWhiteSpace(alias))
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .ToArray();

                    continue;
                }

                merged[key] = existingAliases
                              .Concat(aliases)
                              .Where(alias => !string.IsNullOrWhiteSpace(alias))
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToArray();
            }

            return merged;
        }
    }
}