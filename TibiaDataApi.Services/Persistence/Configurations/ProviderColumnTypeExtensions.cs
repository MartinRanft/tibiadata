using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    internal static class ProviderColumnTypeExtensions
    {
        private const string ProviderNameAnnotation = "TibiaDataApi:ProviderName";

        public static void SetProviderNameAnnotation(this ModelBuilder modelBuilder, string? providerName)
        {
            modelBuilder.Model.SetAnnotation(ProviderNameAnnotation, providerName ?? string.Empty);
        }

        public static PropertyBuilder HasProviderDateTimeColumnType(this PropertyBuilder builder)
        {
            return builder.HasColumnType(ResolveDateTimeColumnType(GetProviderName(builder.Metadata.DeclaringType?.Model)));
        }

        public static PropertyBuilder HasProviderJsonColumnType(this PropertyBuilder builder)
        {
            return builder.HasColumnType(ResolveJsonColumnType(GetProviderName(builder.Metadata.DeclaringType?.Model)));
        }

        public static PropertyBuilder HasProviderLargeTextColumnType(this PropertyBuilder builder)
        {
            return builder.HasColumnType(ResolveLargeTextColumnType(GetProviderName(builder.Metadata.DeclaringType?.Model)));
        }

        public static PropertyBuilder HasProviderDoubleColumnType(this PropertyBuilder builder)
        {
            return builder.HasColumnType(ResolveDoubleColumnType(GetProviderName(builder.Metadata.DeclaringType?.Model)));
        }

        private static string GetProviderName(IReadOnlyModel? model)
        {
            return model?.FindAnnotation(ProviderNameAnnotation)?.Value?.ToString() ?? DatabaseProviderNames.MariaDb;
        }

        private static string ResolveDateTimeColumnType(string providerName)
        {
            return providerName switch
            {
                var value when value.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) => "timestamp with time zone",
                var value when value.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) => "datetime2",
                _ => "datetime(6)"
            };
        }

        private static string ResolveJsonColumnType(string providerName)
        {
            return providerName switch
            {
                var value when value.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) => "jsonb",
                var value when value.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) => "nvarchar(max)",
                _ => "json"
            };
        }

        private static string ResolveLargeTextColumnType(string providerName)
        {
            return providerName switch
            {
                var value when value.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) => "text",
                var value when value.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) => "nvarchar(max)",
                _ => "longtext"
            };
        }

        private static string ResolveDoubleColumnType(string providerName)
        {
            return providerName switch
            {
                var value when value.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) => "double precision",
                var value when value.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) => "float",
                _ => "double"
            };
        }
    }
}
