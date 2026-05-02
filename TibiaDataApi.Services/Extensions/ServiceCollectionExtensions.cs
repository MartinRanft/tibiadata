using Coravel;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using TibiaDataApi.Services.Admin.Security;
using TibiaDataApi.Services.Admin.Statistics;
using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.BackgroundJobs;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.DataBaseService.Assets;
using TibiaDataApi.Services.DataBaseService.Assets.Interfaces;
using TibiaDataApi.Services.DataBaseService.WheelOfDestiny;
using TibiaDataApi.Services.DataBaseService.WheelOfDestiny.Interfaces;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper;
using TibiaDataApi.Services.Scraper.Implementations;
using TibiaDataApi.Services.Scraper.Queries;
using TibiaDataApi.Services.Scraper.Runtime;
using TibiaDataApi.Services.TibiaWiki;
using TibiaDataApi.Services.WheelOfDestiny;
using TibiaDataApi.Services.WheelOfDestiny.Interfaces;

namespace TibiaDataApi.Services.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTibiaDataApiServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            DatabaseOptions databaseOptions = DatabaseConfiguration.GetOptions(configuration);
            CachingOptions cachingOptions = CachingConfiguration.GetOptions(configuration);
            TibiaWikiClientOptions tibiaWikiClientOptions =
            configuration.GetSection(TibiaWikiClientOptions.SectionName).Get<TibiaWikiClientOptions>() ?? new TibiaWikiClientOptions();
            ScraperRuntimeOptions scraperRuntimeOptions =
            configuration.GetSection(ScraperRuntimeOptions.SectionName).Get<ScraperRuntimeOptions>() ?? new ScraperRuntimeOptions();
            BackgroundJobOptions backgroundJobOptions =
            configuration.GetSection(BackgroundJobOptions.SectionName).Get<BackgroundJobOptions>() ?? new BackgroundJobOptions();
            AssetStorageOptions assetStorageOptions =
            configuration.GetSection(AssetStorageOptions.SectionName).Get<AssetStorageOptions>() ?? new AssetStorageOptions();
            string connectionStringName = DatabaseConfiguration.ResolveConnectionStringName(environment, databaseOptions);
            string connectionString = DatabaseConfiguration.GetRequiredConnectionString(configuration, connectionStringName);
            string? redisConnectionString = CachingConfiguration.GetRedisConnectionString(configuration, cachingOptions);

            services.AddSingleton(databaseOptions);
            services.AddSingleton(cachingOptions);
            services.AddSingleton(tibiaWikiClientOptions);
            services.AddSingleton(scraperRuntimeOptions);
            services.AddSingleton(backgroundJobOptions);
            services.AddSingleton(assetStorageOptions);

            if(cachingOptions.UseRedisForHybridCache && !string.IsNullOrWhiteSpace(redisConnectionString))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                    options.InstanceName = $"{cachingOptions.RedisInstanceName}:hybrid:";
                });
            }

            services.AddHybridCache(options =>
            {
                options.MaximumPayloadBytes = cachingOptions.HybridCache.MaximumPayloadBytes;
                options.MaximumKeyLength = cachingOptions.HybridCache.MaximumKeyLength;
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultExpirationSeconds)),
                    LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, cachingOptions.HybridCache.DefaultLocalExpirationSeconds))
                };
            });

            services.AddSingleton<IDatabaseLoadMonitor, DatabaseLoadMonitor>();
            services.AddScoped<DatabaseCommandMetricsInterceptor>();
            services.AddDbContext<TibiaDbContext>((serviceProvider, options) =>
            {
                DatabaseConfiguration.Configure(options, connectionString, databaseOptions.Provider);
                options.AddInterceptors(serviceProvider.GetRequiredService<DatabaseCommandMetricsInterceptor>());
            });

            services.AddScheduler();

            services.AddHttpClient("TibiaWiki",
                client =>
                {
                    client.BaseAddress = new Uri("https://tibia.fandom.com/");
                    client.DefaultRequestHeaders.Add("User-Agent", "TibiaDataApi/1.0");
                    client.Timeout = Timeout.InfiniteTimeSpan;
                });

            services.AddSingleton<ITibiaWikiHttpService, TibiaWikiHttpService>();
            services.AddScoped<IItemImageAssetService, ItemImageAssetService>();
            services.AddScoped<IItemImageSyncService, ItemImageSyncService>();
            services.AddScoped<ICreatureImageAssetService, CreatureImageAssetService>();
            services.AddScoped<ICreatureImageSyncService, CreatureImageSyncService>();
            services.AddTransient<IWikiScraper, SwordScraper>();

            foreach(WikiCategoryDefinition definition in TibiaWikiCategoryCatalog.All
                                                                                 .Where(entry => entry.ContentType == WikiContentType.Item)
                                                                                 .Where(entry => !string.Equals(entry.Slug, "sword-weapons", StringComparison.OrdinalIgnoreCase)))
            {
                services.AddTransient<IWikiScraper>(serviceProvider =>
                new CatalogBackedItemScraper(
                    definition.Slug,
                    serviceProvider.GetRequiredService<ITibiaWikiHttpService>(),
                    serviceProvider.GetRequiredService<IItemImageSyncService>(),
                    serviceProvider.GetRequiredService<ILogger<CatalogBackedItemScraper>>()));
            }

            foreach(WikiCategoryDefinition definition in TibiaWikiCategoryCatalog.All
                                                                                 .Where(entry => entry.ContentType == WikiContentType.Creature))
            {
                services.AddTransient<IWikiScraper>(serviceProvider =>
                new CatalogBackedCreatureScraper(
                    definition.Slug,
                    serviceProvider.GetRequiredService<ITibiaWikiHttpService>(),
                    serviceProvider.GetRequiredService<ICreatureImageSyncService>(),
                    serviceProvider.GetRequiredService<ILogger<CatalogBackedCreatureScraper>>()));
            }

            foreach(WikiCategoryDefinition definition in TibiaWikiCategoryCatalog.All
                                                                                 .Where(entry => entry.ContentType != WikiContentType.Item)
                                                                                 .Where(entry => entry.ContentType != WikiContentType.Creature))
            {
                services.AddTransient<IWikiScraper>(serviceProvider =>
                new CatalogBackedWikiArticleScraper(
                    definition.ContentType,
                    definition.Slug,
                    serviceProvider.GetRequiredService<ITibiaWikiHttpService>(),
                    serviceProvider.GetRequiredService<ILogger<CatalogBackedWikiArticleScraper>>()));
            }

            if (environment.IsDevelopment())
                services.AddScoped<IWheelPlannerLayoutSource, OfficialWheelPlannerLayoutSource>();
            else
                services.AddScoped<IWheelPlannerLayoutSource, EmbeddedWheelPlannerLayoutSource>();

            services.AddScoped<IWheelDataImportService, WheelDataImportService>();
            services.AddScoped<IWheelDataBaseService, WheelDataBaseService>();
            services.AddTransient<IWikiScraper, WheelDataImportScraper>();
            services.AddScoped<BasicModTableScraper>();
            services.AddScoped<SupremeModTableScraper>();
            services.AddScoped<IGemModDataImportService, HybridGemModDataImportService>();
            services.AddScoped<IApiStatisticsService, ApiStatisticsService>();
            services.AddScoped<IAdminCredentialService, AdminCredentialService>();
            services.AddScoped<IAdminLoginProtectionService, AdminLoginProtectionService>();
            services.AddScoped<IIpBanService, IpBanService>();
            RegisterDataBaseServices(services);
            services.AddScoped<IAssetStreamService, AssetStreamService>();
            services.AddScoped<IScraperQueryService, ScraperQueryService>();
            services.AddScoped<WikiCategoryCatalogSynchronizer>();
            services.AddSingleton<ICacheInvalidationService, CacheInvalidationService>();
            services.AddSingleton<IBackgroundJobOrchestrator, BackgroundJobOrchestrator>();
            services.AddScoped<IScheduledScraperConfigurationService, ScheduledScraperConfigurationService>();
            services.AddSingleton<IScraperExecutionLeaseService, ScraperExecutionLeaseService>();
            services.AddSingleton<IScraperRuntimeService, ScraperRuntimeService>();
            services.AddTransient<TibiaScraperJob>();
            services.AddTransient<ItemImageSyncJob>();
            services.AddTransient<CreatureImageSyncJob>();

            return services;
        }

        private static void RegisterDataBaseServices(IServiceCollection services)
        {
            Type[] types = typeof(ServiceCollectionExtensions).Assembly.GetTypes();

            foreach(Type implementationType in types.Where(type => type is { IsClass: true, IsAbstract: false })
                                                    .Where(type => type.Name.EndsWith("DataBaseService", StringComparison.Ordinal))
                                                    .Where(type => type.Namespace?.Contains(".DataBaseService", StringComparison.Ordinal) == true))
            {
                foreach(Type serviceType in implementationType.GetInterfaces()
                                                              .Where(type => type.Namespace?.Contains(".DataBaseService", StringComparison.Ordinal) == true))
                {
                    services.AddScoped(serviceType, implementationType);
                }
            }
        }
    }
}
