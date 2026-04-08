using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace TibiaDataApi.Services.Persistence
{
    public class TibiaDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TibiaDbContext>
    {
        public TibiaDbContext CreateDbContext(string[] args)
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development;
            string apiProjectPath = ResolveApiProjectPath();

            IConfigurationRoot configuration = new ConfigurationBuilder()
                                               .SetBasePath(apiProjectPath)
                                               .AddJsonFile("appsettings.json", false)
                                               .AddJsonFile($"appsettings.{environmentName}.json", true)
                                               .AddEnvironmentVariables()
                                               .Build();

            DatabaseOptions databaseOptions = DatabaseConfiguration.GetOptions(configuration);
            string connectionStringName = DatabaseConfiguration.ResolveConnectionStringName(environmentName, databaseOptions);
            string connectionString = DatabaseConfiguration.GetRequiredConnectionString(configuration, connectionStringName);

            DbContextOptionsBuilder<TibiaDbContext> optionsBuilder = new();
            DatabaseConfiguration.Configure(optionsBuilder, connectionString, databaseOptions.Provider);

            return new TibiaDbContext(optionsBuilder.Options);
        }

        private static string ResolveApiProjectPath()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string[] candidates =
            [
                Path.Combine(currentDirectory, "TibiaDataApi.Api"),
                Path.GetFullPath(Path.Combine(currentDirectory, "..", "TibiaDataApi.Api")),
                Path.GetFullPath(Path.Combine(currentDirectory, "..", "..", "TibiaDataApi.Api"))
            ];

            foreach(string candidate in candidates)
            {
                if(File.Exists(Path.Combine(candidate, "appsettings.json")))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException("Could not resolve the TibiaDataApi.Api project path for EF design-time configuration.");
        }
    }
}