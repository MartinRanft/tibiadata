using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddWheelPerks : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wheel_perks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "varchar(191)", maxLength: 191, nullable: false),
                    Slug = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Vocation = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Summary = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    MainSourceTitle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    MainSourceUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    IsGenericAcrossVocations = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MetadataJson = table.Column<string>(type: "json", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wheel_perks", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wheel_perk_occurrences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    WheelPerkId = table.Column<int>(type: "int", nullable: false),
                    Domain = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    OccurrenceIndex = table.Column<short>(type: "smallint", nullable: false),
                    RequiredPoints = table.Column<short>(type: "smallint", nullable: true),
                    IsStackable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wheel_perk_occurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wheel_perk_occurrences_wheel_perks_WheelPerkId",
                        column: x => x.WheelPerkId,
                        principalTable: "wheel_perks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wheel_perk_stages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    WheelPerkId = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    UnlockKind = table.Column<int>(type: "int", nullable: false),
                    UnlockValue = table.Column<short>(type: "smallint", nullable: false),
                    EffectSummary = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    EffectDetailsJson = table.Column<string>(type: "json", nullable: true),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wheel_perk_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wheel_perk_stages_wheel_perks_WheelPerkId",
                        column: x => x.WheelPerkId,
                        principalTable: "wheel_perks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_wheel_perk_occurrences_WheelPerkId_Domain",
                table: "wheel_perk_occurrences",
                columns: new[] { "WheelPerkId", "Domain" });

            migrationBuilder.CreateIndex(
                name: "IX_wheel_perk_occurrences_WheelPerkId_OccurrenceIndex",
                table: "wheel_perk_occurrences",
                columns: new[] { "WheelPerkId", "OccurrenceIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wheel_perk_stages_WheelPerkId_SortOrder",
                table: "wheel_perk_stages",
                columns: new[] { "WheelPerkId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_wheel_perk_stages_WheelPerkId_Stage",
                table: "wheel_perk_stages",
                columns: new[] { "WheelPerkId", "Stage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wheel_perks_Key",
                table: "wheel_perks",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wheel_perks_Vocation_IsActive",
                table: "wheel_perks",
                columns: new[] { "Vocation", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_wheel_perks_Vocation_Slug",
                table: "wheel_perks",
                columns: new[] { "Vocation", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wheel_perks_Vocation_Type",
                table: "wheel_perks",
                columns: new[] { "Vocation", "Type" });
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wheel_perk_occurrences");

            migrationBuilder.DropTable(
                name: "wheel_perk_stages");

            migrationBuilder.DropTable(
                name: "wheel_perks");
        }
    }
}
