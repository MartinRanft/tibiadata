
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TibiaDataApi.Services.Persistence;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
    [DbContext(typeof(TibiaDbContext))]
    [Migration("20260328212521_AddApiStatsAndIpBans")]
    partial class AddApiStatsAndIpBans
    {
                protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ApiRequestLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

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

                    b.Property<string>("Route")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<int>("StatusCode")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("IpAddress");

                    b.HasIndex("IsBlocked");

                    b.HasIndex("OccurredAt");

                    b.HasIndex("Method", "Route");

                    b.ToTable("api_request_logs", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Creature", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

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

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("creatures", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.IpBan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

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

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Item", b =>
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

                    b.ToTable("items", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ItemCategory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

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

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("WikiCategoryName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.HasIndex("WikiCategoryName")
                        .IsUnique();

                    b.ToTable("item_categories", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ScrapeError", b =>
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

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ScrapeItemChange", b =>
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

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ScrapeLog", b =>
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

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Item", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.ItemCategory", "Category")
                        .WithMany("Items")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Category");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ScrapeError", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.ScrapeLog", "ScrapeLog")
                        .WithMany("Errors")
                        .HasForeignKey("ScrapeLogId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ScrapeLog");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ScrapeItemChange", b =>
                {
                    b.HasOne("TibiaDataApi.Services.Entities.ScrapeLog", "ScrapeLog")
                        .WithMany("ItemChanges")
                        .HasForeignKey("ScrapeLogId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ScrapeLog");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ItemCategory", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ScrapeLog", b =>
                {
                    b.Navigation("Errors");

                    b.Navigation("ItemChanges");
                });
#pragma warning restore 612, 618
        }
    }
}
