
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TibiaDataApi.Services.Persistence;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
    [DbContext(typeof(TibiaDbContext))]
    partial class TibiaDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("TibiaDataApi:ProviderName", "MySql.EntityFrameworkCore");

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.Asset", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ContentSha256")
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<DateTime>("DownloadedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Extension")
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<int?>("Height")
                        .HasColumnType("int");

                    b.Property<string>("MimeType")
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<long>("SizeBytes")
                        .HasColumnType("bigint");

                    b.Property<string>("SourceFileTitle")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("SourcePageTitle")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("SourceSha1")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("SourceUrl")
                        .HasMaxLength(1024)
                        .HasColumnType("varchar(1024)");

                    b.Property<string>("StorageKey")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("varchar(512)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("Width")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ContentSha256");

                    b.HasIndex("StorageKey")
                        .IsUnique();

                    b.ToTable("assets", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.CreatureAsset", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AssetId")
                        .HasColumnType("int");

                    b.Property<int>("AssetKind")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatureId")
                        .HasColumnType("int");

                    b.Property<bool>("IsPrimary")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("AssetId");

                    b.HasIndex("CreatureId", "AssetId")
                        .IsUnique();

                    b.HasIndex("CreatureId", "AssetKind", "SortOrder");

                    b.ToTable("creature_assets", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.CreatureImageSyncQueueEntry", b =>
                {
                    b.Property<int>("CreatureId")
                        .HasColumnType("int");

                    b.Property<string>("ErrorMessage")
                        .HasMaxLength(2000)
                        .HasColumnType("varchar(2000)");

                    b.Property<DateTime?>("LastAttemptedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("LastCompletedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("RequestedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("RetryCount")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("WikiPageTitle")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("CreatureId");

                    b.HasIndex("RequestedAt");

                    b.HasIndex("Status");

                    b.HasIndex("UpdatedAt");

                    b.ToTable("creature_image_sync_queue", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.ItemAsset", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AssetId")
                        .HasColumnType("int");

                    b.Property<int>("AssetKind")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsPrimary")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("ItemId")
                        .HasColumnType("int");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("AssetId");

                    b.HasIndex("ItemId", "AssetId")
                        .IsUnique();

                    b.HasIndex("ItemId", "AssetKind", "SortOrder");

                    b.ToTable("item_assets", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.ItemImageSyncQueueEntry", b =>
                {
                    b.Property<int>("ItemId")
                        .HasColumnType("int");

                    b.Property<string>("ErrorMessage")
                        .HasMaxLength(2000)
                        .HasColumnType("varchar(2000)");

                    b.Property<DateTime?>("LastAttemptedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("LastCompletedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("RequestedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("RetryCount")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("WikiPageTitle")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("ItemId");

                    b.HasIndex("RequestedAt");

                    b.HasIndex("Status");

                    b.HasIndex("UpdatedAt");

                    b.ToTable("item_image_sync_queue", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Categories.WikiCategory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("GroupName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("GroupSlug")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("ObjectClass")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("SourceKind")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("SourceSection")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("SourceTitle")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.HasIndex("ContentType", "GroupSlug");

                    b.HasIndex("ContentType", "SortOrder");

                    b.ToTable("wiki_categories", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Content.WikiArticle", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("AdditionalAttributesJson")
                        .HasColumnType("json");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("InfoboxJson")
                        .HasColumnType("json");

                    b.Property<string>("InfoboxTemplate")
                        .HasColumnType("longtext");

                    b.Property<bool>("IsMissingFromSource")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime?>("LastSeenAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("LinkedTitles")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<DateTime?>("MissingSince")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("NormalizedTitle")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("PlainTextContent")
                        .HasColumnType("longtext");

                    b.Property<string>("RawWikiText")
                        .HasColumnType("longtext");

                    b.Property<string>("Sections")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("Summary")
                        .HasMaxLength(8000)
                        .HasColumnType("varchar(8000)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("WikiUrl")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ContentType", "NormalizedTitle")
                        .IsUnique();

                    b.ToTable("wiki_articles", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Content.WikiArticleCategory", b =>
                {
                    b.Property<int>("WikiArticleId")
                        .HasColumnType("int");

                    b.Property<int>("WikiCategoryId")
                        .HasColumnType("int");

                    b.Property<DateTime>("FirstSeenAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsMissingFromSource")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime?>("LastSeenAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("MissingSince")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("WikiArticleId", "WikiCategoryId");

                    b.HasIndex("IsMissingFromSource");

                    b.HasIndex("LastSeenAt");

                    b.HasIndex("WikiCategoryId");

                    b.ToTable("wiki_article_categories", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Creatures.Creature", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("BestiaryJson")
                        .HasColumnType("json");

                    b.Property<long>("Experience")
                        .HasColumnType("bigint");

                    b.Property<int>("Hitpoints")
                        .HasColumnType("int");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("LootStatisticsJson")
                        .HasColumnType("json");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("NormalizedName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("NormalizedName")
                        .IsUnique();

                    b.ToTable("creatures", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Items.Item", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ActualName")
                        .HasColumnType("longtext");

                    b.Property<string>("AdditionalAttributesJson")
                        .HasColumnType("json");

                    b.Property<string>("Armor")
                        .HasColumnType("longtext");

                    b.Property<string>("Article")
                        .HasColumnType("longtext");

                    b.Property<string>("Attack")
                        .HasColumnType("longtext");

                    b.Property<string>("Attrib")
                        .HasColumnType("longtext");

                    b.Property<int?>("CategoryId")
                        .HasColumnType("int");

                    b.Property<string>("DamageRange")
                        .HasColumnType("longtext");

                    b.Property<string>("DamageType")
                        .HasColumnType("longtext");

                    b.Property<string>("DeathAttack")
                        .HasColumnType("longtext");

                    b.Property<string>("Defense")
                        .HasColumnType("longtext");

                    b.Property<string>("DefenseMod")
                        .HasColumnType("longtext");

                    b.Property<string>("DroppedBy")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("EarthAttack")
                        .HasColumnType("longtext");

                    b.Property<string>("EnergyAttack")
                        .HasColumnType("longtext");

                    b.Property<string>("FireAttack")
                        .HasColumnType("longtext");

                    b.Property<string>("Hands")
                        .HasColumnType("longtext");

                    b.Property<string>("HolyAttack")
                        .HasColumnType("longtext");

                    b.Property<string>("IceAttack")
                        .HasColumnType("longtext");

                    b.Property<string>("ImbueSlots")
                        .HasColumnType("longtext");

                    b.Property<string>("Implemented")
                        .HasColumnType("longtext");

                    b.Property<bool>("IsMissingFromSource")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("ItemId")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<DateTime?>("LastSeenAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("LevelRequired")
                        .HasColumnType("longtext");

                    b.Property<string>("Marketable")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("MissingSince")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("NormalizedActualName")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("NormalizedName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("NpcPrice")
                        .HasColumnType("longtext");

                    b.Property<string>("NpcValue")
                        .HasColumnType("longtext");

                    b.Property<string>("ObjectClass")
                        .HasColumnType("longtext");

                    b.Property<string>("Plural")
                        .HasColumnType("longtext");

                    b.Property<string>("PrimaryType")
                        .HasColumnType("longtext");

                    b.Property<string>("Range")
                        .HasColumnType("longtext");

                    b.Property<string>("SecondaryType")
                        .HasColumnType("longtext");

                    b.Property<string>("Sounds")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("Stackable")
                        .HasColumnType("longtext");

                    b.Property<string>("TemplateType")
                        .HasColumnType("longtext");

                    b.Property<string>("UpgradeClass")
                        .HasColumnType("longtext");

                    b.Property<string>("Usable")
                        .HasColumnType("longtext");

                    b.Property<string>("Value")
                        .HasColumnType("longtext");

                    b.Property<string>("Vocation")
                        .HasColumnType("longtext");

                    b.Property<string>("Walkable")
                        .HasColumnType("longtext");

                    b.Property<string>("WeaponType")
                        .HasColumnType("longtext");

                    b.Property<string>("Weight")
                        .HasColumnType("longtext");

                    b.Property<string>("WikiUrl")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("NormalizedActualName");

                    b.HasIndex("NormalizedName")
                        .IsUnique();

                    b.ToTable("items", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Monitoring.ApiRequestDailyAggregate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("BlockedCount")
                        .HasColumnType("int");

                    b.Property<int>("CacheBypassCount")
                        .HasColumnType("int");

                    b.Property<int>("CacheHitCount")
                        .HasColumnType("int");

                    b.Property<int>("CacheMissCount")
                        .HasColumnType("int");

                    b.Property<DateTime>("DayUtc")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("ErrorCount")
                        .HasColumnType("int");

                    b.Property<int>("RequestCount")
                        .HasColumnType("int");

                    b.Property<double>("TotalDurationMs")
                        .HasColumnType("double");

                    b.Property<long>("TotalResponseSizeBytes")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("DayUtc")
                        .IsUnique();

                    b.ToTable("api_request_daily_aggregates", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Monitoring.ApiRequestLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("CacheStatus")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.Property<double>("DurationMs")
                        .HasColumnType("double");

                    b.Property<string>("IpAddress")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("IsBlocked")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Method")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("varchar(16)");

                    b.Property<DateTime>("OccurredAt")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("ResponseSizeBytes")
                        .HasColumnType("bigint");

                    b.Property<string>("Route")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<int>("StatusCode")
                        .HasColumnType("int");

                    b.Property<string>("UserAgent")
                        .HasMaxLength(512)
                        .HasColumnType("varchar(512)");

                    b.HasKey("Id");

                    b.HasIndex("CacheStatus");

                    b.HasIndex("IpAddress");

                    b.HasIndex("IsBlocked");

                    b.HasIndex("OccurredAt");

                    b.HasIndex("Method", "Route");

                    b.ToTable("api_request_logs", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Monitoring.BackgroundJobExecution", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<double?>("DurationMs")
                        .HasColumnType("double");

                    b.Property<int>("FailedCount")
                        .HasColumnType("int");

                    b.Property<DateTime?>("FinishedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("JobName")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("LeaseName")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("LeaseOwnerId")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Message")
                        .HasMaxLength(2000)
                        .HasColumnType("varchar(2000)");

                    b.Property<string>("MetadataJson")
                        .HasColumnType("json");

                    b.Property<int>("ProcessedCount")
                        .HasColumnType("int");

                    b.Property<int>("SkippedCount")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("SucceededCount")
                        .HasColumnType("int");

                    b.Property<string>("TriggeredBy")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("JobName");

                    b.HasIndex("StartedAt");

                    b.HasIndex("Status");

                    b.HasIndex("JobName", "StartedAt");

                    b.ToTable("background_job_executions", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Monitoring.ScheduledScraperConfiguration", b =>
                {
                    b.Property<string>("Key")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Enabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime?>("LastTriggeredAtUtc")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("ScheduleHour")
                        .HasColumnType("int");

                    b.Property<int>("ScheduleMinute")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Key");

                    b.ToTable("scheduled_scraper_configurations", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Scraping.ScrapeError", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("DetailsJson")
                        .HasColumnType("json");

                    b.Property<string>("ErrorType")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("ItemName")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("varchar(2000)");

                    b.Property<DateTime>("OccurredAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("PageTitle")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Scope")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<int>("ScrapeLogId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("OccurredAt");

                    b.HasIndex("Scope");

                    b.HasIndex("ScrapeLogId");

                    b.ToTable("scrape_errors", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Scraping.ScrapeItemChange", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("AfterJson")
                        .HasColumnType("json");

                    b.Property<string>("BeforeJson")
                        .HasColumnType("json");

                    b.Property<string>("CategoryName")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("CategorySlug")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("ChangeType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("ChangedFieldsJson")
                        .HasColumnType("json");

                    b.Property<string>("ErrorMessage")
                        .HasMaxLength(2000)
                        .HasColumnType("varchar(2000)");

                    b.Property<int?>("ItemId")
                        .HasColumnType("int");

                    b.Property<string>("ItemName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<DateTime>("OccurredAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("ScrapeLogId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ChangeType");

                    b.HasIndex("ItemId");

                    b.HasIndex("OccurredAt");

                    b.HasIndex("ScrapeLogId");

                    b.ToTable("scrape_item_changes", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Scraping.ScrapeLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("CategoryName")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("CategorySlug")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("ChangesJson")
                        .HasColumnType("json");

                    b.Property<string>("ErrorMessage")
                        .HasMaxLength(2000)
                        .HasColumnType("varchar(2000)");

                    b.Property<string>("ErrorType")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<DateTime?>("FinishedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("ItemsAdded")
                        .HasColumnType("int");

                    b.Property<int>("ItemsFailed")
                        .HasColumnType("int");

                    b.Property<int>("ItemsMissingFromSource")
                        .HasColumnType("int");

                    b.Property<int>("ItemsProcessed")
                        .HasColumnType("int");

                    b.Property<int>("ItemsUnchanged")
                        .HasColumnType("int");

                    b.Property<int>("ItemsUpdated")
                        .HasColumnType("int");

                    b.Property<string>("MetadataJson")
                        .HasColumnType("json");

                    b.Property<int>("PagesDiscovered")
                        .HasColumnType("int");

                    b.Property<int>("PagesFailed")
                        .HasColumnType("int");

                    b.Property<int>("PagesProcessed")
                        .HasColumnType("int");

                    b.Property<string>("ScraperName")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<bool>("Success")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("TriggeredBy")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("StartedAt");

                    b.HasIndex("Status");

                    b.ToTable("scrape_logs", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Scraping.ScraperExecutionLease", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<DateTime>("AcquiredAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("OwnerId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Name");

                    b.HasIndex("ExpiresAt");

                    b.HasIndex("UpdatedAt");

                    b.ToTable("scraper_execution_leases", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Security.AdminCredential", b =>
                {
                    b.Property<string>("Key")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("varchar(512)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Key");

                    b.ToTable("admin_credentials", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Security.AdminLoginFailure", b =>
                {
                    b.Property<string>("IpAddress")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<int>("FailedAttempts")
                        .HasColumnType("int");

                    b.Property<DateTime>("FirstFailedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("LastFailedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("IpAddress");

                    b.ToTable("admin_login_failures", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Security.IpBan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<int?>("DurationMinutes")
                        .HasColumnType("int");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("IpAddress")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<string>("RevocationReason")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<DateTime?>("RevokedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("RevokedBy")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("IpAddress");

                    b.HasIndex("IpAddress", "IsActive");

                    b.ToTable("ip_bans", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Security.RequestProtectionConfiguration", b =>
                {
                    b.Property<string>("Key")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<int>("AdminLoginConcurrentPermitLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminLoginConcurrentQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminLoginReplenishmentSeconds")
                        .HasColumnType("int");

                    b.Property<int>("AdminLoginTokenLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminLoginTokenQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminLoginTokensPerPeriod")
                        .HasColumnType("int");

                    b.Property<int>("AdminMutationApiConcurrentPermitLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminMutationApiConcurrentQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminMutationApiReplenishmentSeconds")
                        .HasColumnType("int");

                    b.Property<int>("AdminMutationApiTokenLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminMutationApiTokenQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminMutationApiTokensPerPeriod")
                        .HasColumnType("int");

                    b.Property<int>("AdminReadApiConcurrentPermitLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminReadApiConcurrentQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminReadApiReplenishmentSeconds")
                        .HasColumnType("int");

                    b.Property<int>("AdminReadApiTokenLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminReadApiTokenQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("AdminReadApiTokensPerPeriod")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Enabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("HealthApiConcurrentPermitLimit")
                        .HasColumnType("int");

                    b.Property<int>("HealthApiConcurrentQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("HealthApiReplenishmentSeconds")
                        .HasColumnType("int");

                    b.Property<int>("HealthApiTokenLimit")
                        .HasColumnType("int");

                    b.Property<int>("HealthApiTokenQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("HealthApiTokensPerPeriod")
                        .HasColumnType("int");

                    b.Property<int>("PublicApiConcurrentPermitLimit")
                        .HasColumnType("int");

                    b.Property<int>("PublicApiConcurrentQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("PublicApiReplenishmentSeconds")
                        .HasColumnType("int");

                    b.Property<int>("PublicApiTokenLimit")
                        .HasColumnType("int");

                    b.Property<int>("PublicApiTokenQueueLimit")
                        .HasColumnType("int");

                    b.Property<int>("PublicApiTokensPerPeriod")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.HasKey("Key");

                    b.ToTable("request_protection_configurations", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.CreatureAsset", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.Assets.Asset", "Asset")
                        .WithMany("CreatureAssets")
                        .HasForeignKey("AssetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TibiaDataApi.Services.Entities.Creatures.Creature", "Creature")
                        .WithMany("CreatureAssets")
                        .HasForeignKey("CreatureId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Asset");

                    b.Navigation("Creature");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.CreatureImageSyncQueueEntry", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.Creatures.Creature", "Creature")
                        .WithOne("ImageSyncQueueEntry")
                        .HasForeignKey("TibiaDataApi.Services.Entities.Assets.CreatureImageSyncQueueEntry", "CreatureId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Creature");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.ItemAsset", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.Assets.Asset", "Asset")
                        .WithMany("ItemAssets")
                        .HasForeignKey("AssetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TibiaDataApi.Services.Entities.Items.Item", "Item")
                        .WithMany("ItemAssets")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Asset");

                    b.Navigation("Item");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.ItemImageSyncQueueEntry", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.Items.Item", "Item")
                        .WithOne("ImageSyncQueueEntry")
                        .HasForeignKey("TibiaDataApi.Services.Entities.Assets.ItemImageSyncQueueEntry", "ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Content.WikiArticleCategory", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.Content.WikiArticle", "WikiArticle")
                        .WithMany("WikiArticleCategories")
                        .HasForeignKey("WikiArticleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TibiaDataApi.Services.Entities.Categories.WikiCategory", "WikiCategory")
                        .WithMany("WikiArticleCategories")
                        .HasForeignKey("WikiCategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("WikiArticle");

                    b.Navigation("WikiCategory");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Items.Item", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.Categories.WikiCategory", "Category")
                        .WithMany("Items")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Category");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Scraping.ScrapeError", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.Scraping.ScrapeLog", "ScrapeLog")
                        .WithMany("Errors")
                        .HasForeignKey("ScrapeLogId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ScrapeLog");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Scraping.ScrapeItemChange", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.Scraping.ScrapeLog", "ScrapeLog")
                        .WithMany("ItemChanges")
                        .HasForeignKey("ScrapeLogId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ScrapeLog");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Assets.Asset", b =>
                {
                    b.Navigation("CreatureAssets");

                    b.Navigation("ItemAssets");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Categories.WikiCategory", b =>
                {
                    b.Navigation("Items");

                    b.Navigation("WikiArticleCategories");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Content.WikiArticle", b =>
                {
                    b.Navigation("WikiArticleCategories");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Creatures.Creature", b =>
                {
                    b.Navigation("CreatureAssets");

                    b.Navigation("ImageSyncQueueEntry");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Items.Item", b =>
                {
                    b.Navigation("ImageSyncQueueEntry");

                    b.Navigation("ItemAssets");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Scraping.ScrapeLog", b =>
                {
                    b.Navigation("Errors");

                    b.Navigation("ItemChanges");
                });
#pragma warning restore 612, 618
        }
    }
}
