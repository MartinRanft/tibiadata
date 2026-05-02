using Microsoft.EntityFrameworkCore;

using TibiaDataApi.Services.Entities.Assets;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Entities.Monitoring;
using TibiaDataApi.Services.Entities.Scraping;
using TibiaDataApi.Services.Entities.Security;
using TibiaDataApi.Services.Entities.WheelOfDestiny;
using TibiaDataApi.Services.Persistence.Configurations;

namespace TibiaDataApi.Services.Persistence
{
    public class TibiaDbContext(DbContextOptions<TibiaDbContext> options) : DbContext(options)
    {
        public DbSet<Item> Items { get; set; }

        public DbSet<Asset> Assets { get; set; }

        public DbSet<ItemAsset> ItemAssets { get; set; }

        public DbSet<CreatureAsset> CreatureAssets { get; set; }

        public DbSet<ItemImageSyncQueueEntry> ItemImageSyncQueueEntries { get; set; }

        public DbSet<CreatureImageSyncQueueEntry> CreatureImageSyncQueueEntries { get; set; }

        public DbSet<WikiArticle> WikiArticles { get; set; }

        public DbSet<WikiArticleCategory> WikiArticleCategories { get; set; }

        public DbSet<WikiCategory> WikiCategories { get; set; }

        public DbSet<Creature> Creatures { get; set; }

        public DbSet<BackgroundJobExecution> BackgroundJobExecutions { get; set; }

        public DbSet<ScheduledScraperConfiguration> ScheduledScraperConfigurations { get; set; }

        public DbSet<ScrapeLog> ScrapeLogs { get; set; }

        public DbSet<ScrapeItemChange> ScrapeItemChanges { get; set; }

        public DbSet<ScrapeError> ScrapeErrors { get; set; }

        public DbSet<ScraperExecutionLease> ScraperExecutionLeases { get; set; }

        public DbSet<ApiRequestLog> ApiRequestLogs { get; set; }

        public DbSet<ApiRequestDailyAggregate> ApiRequestDailyAggregates { get; set; }

        public DbSet<IpBan> IpBans { get; set; }

        public DbSet<AdminCredential> AdminCredentials { get; set; }

        public DbSet<AdminLoginFailure> AdminLoginFailures { get; set; }

        public DbSet<RequestProtectionConfiguration> RequestProtectionConfigurations { get; set; }

        public DbSet<WheelPerk> WheelPerks { get; set; }

        public DbSet<WheelPerkOccurrence> WheelPerkOccurrences { get; set; }

        public DbSet<WheelPerkStage> WheelPerkStages { get; set; }

        public DbSet<WheelSection> WheelSections { get; set; }

        public DbSet<WheelSectionDedicationPerk> WheelSectionDedicationPerks { get; set; }

        public DbSet<WheelRevelationSlot> WheelRevelationSlots { get; set; }

        public DbSet<Gem> Gems { get; set; }

        public DbSet<GemModifier> GemModifiers { get; set; }

        public DbSet<GemModifierGrade> GemModifierGrades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.SetProviderNameAnnotation(Database.ProviderName);

            
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TibiaDbContext).Assembly);
        }
    }
}
