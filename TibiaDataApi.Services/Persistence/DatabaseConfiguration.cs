using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using MySql.Data.MySqlClient;

namespace TibiaDataApi.Services.Persistence
{
    public static class DatabaseConfiguration
    {
        public static DatabaseOptions GetOptions(IConfiguration configuration)
        {
            return configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        }

        public static string ResolveConnectionStringName(IHostEnvironment environment, DatabaseOptions options)
        {
            return ResolveConnectionStringName(environment.EnvironmentName, options);
        }

        public static string ResolveConnectionStringName(string environmentName, DatabaseOptions options)
        {
            if(string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
            {
                return options.DevelopmentConnectionStringName;
            }

            if(string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                return options.TestConnectionStringName;
            }

            return options.ProductionConnectionStringName;
        }

        public static string GetRequiredConnectionString(IConfiguration configuration, string connectionStringName)
        {
            string? connectionString = configuration.GetConnectionString(connectionStringName);

            if(string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Missing connection string '{connectionStringName}'. Configure it via appsettings or environment variables.");
            }

            return connectionString;
        }

        public static void Configure(DbContextOptionsBuilder optionsBuilder, string connectionString, string provider)
        {
            switch (provider.Trim().ToLowerInvariant())
            {
                case "mariadb":
                case "mysql":
                    optionsBuilder.UseMySQL(NormalizeMySqlConnectionString(connectionString),
                        builder =>
                        {
                            builder.MigrationsAssembly(typeof(TibiaDbContext).Assembly.FullName);
                            builder.EnableRetryOnFailure();
                        });
                    return;

                case "postgresql":
                    optionsBuilder.UseNpgsql(
                        connectionString,
                        builder =>
                        {
                            builder.MigrationsAssembly(typeof(TibiaDbContext).Assembly.FullName);
                            builder.EnableRetryOnFailure();
                        });
                    return;

                case "sqlserver":
                    optionsBuilder.UseSqlServer(
                        connectionString,
                        builder =>
                        {
                            builder.MigrationsAssembly(typeof(TibiaDbContext).Assembly.FullName);
                            builder.EnableRetryOnFailure();
                        });
                    return;

                default:
                    throw new NotSupportedException(
                        $"Unsupported database provider '{provider}'. Supported right now: {DatabaseProviderNames.MariaDb}, {DatabaseProviderNames.MySql}, {DatabaseProviderNames.PostgreSql}, {DatabaseProviderNames.SqlServer}.");
            }
        }

        internal static string NormalizeMySqlConnectionString(string connectionString)
        {
            MySqlConnectionStringBuilder builder = new(connectionString);

            if(string.IsNullOrWhiteSpace(builder.CharacterSet) ||
               string.Equals(builder.CharacterSet, "utf8", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(builder.CharacterSet, "utf8mb3", StringComparison.OrdinalIgnoreCase))
            {
                builder.CharacterSet = "utf8mb4";
            }

            return builder.ConnectionString;
        }
    }
}
