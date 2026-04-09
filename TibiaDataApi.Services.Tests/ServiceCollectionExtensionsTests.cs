using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using TibiaDataApi.Services.Extensions;
using TibiaDataApi.Services.Scraper;

namespace TibiaDataApi.Services.Tests
{
    public sealed class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddTibiaDataApiServices_RegistersOneScraperPerCatalogCategory()
        {
            Dictionary<string, string?> values = new()
            {
                ["Database:Provider"] = "MariaDb",
                ["Database:ProductionConnectionStringName"] = "DatabaseConnection",
                ["Database:DevelopmentConnectionStringName"] = "DatabaseConnectionDev",
                ["Database:TestConnectionStringName"] = "DatabaseConnectionTest",
                ["ConnectionStrings:DatabaseConnection"] = "Server=127.0.0.1;Port=65535;Database=tibiadataapi_prod;User=test;Password=test;",
                ["ConnectionStrings:DatabaseConnectionDev"] = "Server=127.0.0.1;Port=65535;Database=tibiadataapi_dev;User=test;Password=test;",
                ["ConnectionStrings:DatabaseConnectionTest"] = "Server=127.0.0.1;Port=65535;Database=tibiadataapi_test;User=test;Password=test;",
                ["Assets:StorageRootPath"] = "data/test-assets",
                ["Caching:UseRedisForHybridCache"] = "false",
                ["Caching:UseRedisForOutputCache"] = "false"
            };

            IConfiguration configuration = new ConfigurationBuilder()
                                           .AddInMemoryCollection(values)
                                           .Build();

            ServiceCollection services = new();
            services.AddLogging();
            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

            services.AddTibiaDataApiServices(configuration, new TestHostEnvironment());

            using ServiceProvider provider = services.BuildServiceProvider();
            List<IWikiScraper> scrapers = provider.GetRequiredService<IEnumerable<IWikiScraper>>().ToList();

            List<string> expectedCategorySlugs = TibiaWikiCategoryCatalog.All
                                                                         .Select(entry => entry.Slug)
                                                                         .OrderBy(slug => slug, StringComparer.OrdinalIgnoreCase)
                                                                         .ToList();

            List<string> registeredCategorySlugs = scrapers
                                                   .Select(scraper => scraper.RuntimeCategorySlug)
                                                   .OrderBy(slug => slug, StringComparer.OrdinalIgnoreCase)
                                                   .ToList();

            Assert.Equal(expectedCategorySlugs.Count, scrapers.Count);
            Assert.Equal(expectedCategorySlugs, registeredCategorySlugs);
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;

            public string ApplicationName { get; set; } = "TibiaDataApi.Services.Tests";

            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}