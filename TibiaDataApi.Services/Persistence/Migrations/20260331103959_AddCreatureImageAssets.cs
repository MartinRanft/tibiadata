using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddCreatureImageAssets : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "creature_assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CreatureId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    AssetKind = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_creature_assets_assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_creature_assets_creatures_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "creatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "creature_image_sync_queue",
                columns: table => new
                {
                    CreatureId = table.Column<int>(type: "int", nullable: false),
                    WikiPageTitle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastAttemptedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastCompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_image_sync_queue", x => x.CreatureId);
                    table.ForeignKey(
                        name: "FK_creature_image_sync_queue_creatures_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "creatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_creature_assets_AssetId",
                table: "creature_assets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_creature_assets_CreatureId_AssetId",
                table: "creature_assets",
                columns: new[] { "CreatureId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_creature_assets_CreatureId_AssetKind_SortOrder",
                table: "creature_assets",
                columns: new[] { "CreatureId", "AssetKind", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_creature_image_sync_queue_RequestedAt",
                table: "creature_image_sync_queue",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_creature_image_sync_queue_Status",
                table: "creature_image_sync_queue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_creature_image_sync_queue_UpdatedAt",
                table: "creature_image_sync_queue",
                column: "UpdatedAt");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creature_assets");

            migrationBuilder.DropTable(
                name: "creature_image_sync_queue");
        }
    }
}
