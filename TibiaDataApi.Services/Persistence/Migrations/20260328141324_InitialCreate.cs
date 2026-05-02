using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class InitialCreate : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "creatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Hitpoints = table.Column<int>(type: "int", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    LootStatisticsJson = table.Column<string>(type: "json", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creatures", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false),
                    ActualName = table.Column<string>(type: "longtext", nullable: true),
                    Plural = table.Column<string>(type: "longtext", nullable: true),
                    Article = table.Column<string>(type: "longtext", nullable: true),
                    Implemented = table.Column<string>(type: "longtext", nullable: true),
                    ItemId = table.Column<string>(type: "json", nullable: false),
                    DroppedBy = table.Column<string>(type: "json", nullable: false),
                    Sounds = table.Column<string>(type: "json", nullable: false),
                    TemplateType = table.Column<string>(type: "longtext", nullable: true),
                    ObjectClass = table.Column<string>(type: "longtext", nullable: true),
                    PrimaryType = table.Column<string>(type: "longtext", nullable: true),
                    SecondaryType = table.Column<string>(type: "longtext", nullable: true),
                    WeaponType = table.Column<string>(type: "longtext", nullable: true),
                    Hands = table.Column<string>(type: "longtext", nullable: true),
                    Attack = table.Column<string>(type: "longtext", nullable: true),
                    Defense = table.Column<string>(type: "longtext", nullable: true),
                    DefenseMod = table.Column<string>(type: "longtext", nullable: true),
                    Armor = table.Column<string>(type: "longtext", nullable: true),
                    Range = table.Column<string>(type: "longtext", nullable: true),
                    LevelRequired = table.Column<string>(type: "longtext", nullable: true),
                    ImbueSlots = table.Column<string>(type: "longtext", nullable: true),
                    Vocation = table.Column<string>(type: "longtext", nullable: true),
                    DamageType = table.Column<string>(type: "longtext", nullable: true),
                    DamageRange = table.Column<string>(type: "longtext", nullable: true),
                    EnergyAttack = table.Column<string>(type: "longtext", nullable: true),
                    FireAttack = table.Column<string>(type: "longtext", nullable: true),
                    EarthAttack = table.Column<string>(type: "longtext", nullable: true),
                    IceAttack = table.Column<string>(type: "longtext", nullable: true),
                    DeathAttack = table.Column<string>(type: "longtext", nullable: true),
                    HolyAttack = table.Column<string>(type: "longtext", nullable: true),
                    Stackable = table.Column<string>(type: "longtext", nullable: true),
                    Usable = table.Column<string>(type: "longtext", nullable: true),
                    Marketable = table.Column<string>(type: "longtext", nullable: true),
                    Walkable = table.Column<string>(type: "longtext", nullable: true),
                    NpcPrice = table.Column<string>(type: "longtext", nullable: true),
                    NpcValue = table.Column<string>(type: "longtext", nullable: true),
                    Value = table.Column<string>(type: "longtext", nullable: true),
                    Weight = table.Column<string>(type: "longtext", nullable: true),
                    Attrib = table.Column<string>(type: "longtext", nullable: true),
                    UpgradeClass = table.Column<string>(type: "longtext", nullable: true),
                    WikiUrl = table.Column<string>(type: "longtext", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scrape_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    ItemsProcessed = table.Column<int>(type: "int", nullable: false),
                    ItemsAdded = table.Column<int>(type: "int", nullable: false),
                    ItemsUpdated = table.Column<int>(type: "int", nullable: false),
                    ChangesJson = table.Column<string>(type: "json", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrape_logs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_creatures_Name",
                table: "creatures",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_Name",
                table: "items",
                column: "Name",
                unique: true);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creatures");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "scrape_logs");
        }
    }
}
