
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
    [Migration("20260328141324_InitialCreate")]
    partial class InitialCreate
    {
                protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

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

            modelBuilder.Entity("TibiaDataApi.Services.Entities.Item", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ActualName")
                        .HasColumnType("longtext");

                    b.Property<string>("Armor")
                        .HasColumnType("longtext");

                    b.Property<string>("Article")
                        .HasColumnType("longtext");

                    b.Property<string>("Attack")
                        .HasColumnType("longtext");

                    b.Property<string>("Attrib")
                        .HasColumnType("longtext");

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

                    b.Property<string>("ItemId")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("LevelRequired")
                        .HasColumnType("longtext");

                    b.Property<string>("Marketable")
                        .HasColumnType("longtext");

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

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("items", (string)null);
                });

            modelBuilder.Entity("TibiaDataApi.Services.Entities.ScrapeLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ChangesJson")
                        .HasColumnType("json");

                    b.Property<string>("ErrorMessage")
                        .HasMaxLength(2000)
                        .HasColumnType("varchar(2000)");

                    b.Property<DateTime?>("FinishedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("ItemsAdded")
                        .HasColumnType("int");

                    b.Property<int>("ItemsProcessed")
                        .HasColumnType("int");

                    b.Property<int>("ItemsUpdated")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Success")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.ToTable("scrape_logs", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
