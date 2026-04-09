using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddItemAssets : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    StorageKey = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    SourcePageTitle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    SourceFileTitle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    SourceUrl = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    MimeType = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    Extension = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    SourceSha1 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    ContentSha256 = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    DownloadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    AssetKind = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_item_assets_assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_item_assets_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_assets_ContentSha256",
                table: "assets",
                column: "ContentSha256");

            migrationBuilder.CreateIndex(
                name: "IX_assets_StorageKey",
                table: "assets",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_assets_AssetId",
                table: "item_assets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_item_assets_ItemId_AssetId",
                table: "item_assets",
                columns: new[] { "ItemId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_assets_ItemId_AssetKind_SortOrder",
                table: "item_assets",
                columns: new[] { "ItemId", "AssetKind", "SortOrder" });
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_assets");

            migrationBuilder.DropTable(
                name: "assets");
        }
    }
}
