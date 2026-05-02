namespace TibiaDataApi.Services.Text
{
    public static class EntityNameNormalizer
    {
        public static string Normalize(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace('_', ' ')
                        .Trim()
                        .ToLowerInvariant();
        }

        public static string? NormalizeOptional(string? value)
        {
            string normalized = Normalize(value ?? string.Empty);
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }
    }
}