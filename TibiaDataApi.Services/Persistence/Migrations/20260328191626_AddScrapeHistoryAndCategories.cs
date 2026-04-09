using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddScrapeHistoryAndCategories : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "scrape_logs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategorySlug",
                table: "scrape_logs",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorType",
                table: "scrape_logs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemsFailed",
                table: "scrape_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItemsMissingFromSource",
                table: "scrape_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItemsUnchanged",
                table: "scrape_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "scrape_logs",
                type: "json",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PagesDiscovered",
                table: "scrape_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PagesFailed",
                table: "scrape_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PagesProcessed",
                table: "scrape_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScraperName",
                table: "scrape_logs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "scrape_logs",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TriggeredBy",
                table: "scrape_logs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalAttributesJson",
                table: "items",
                type: "json",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMissingFromSource",
                table: "items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "items",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MissingSince",
                table: "items",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "item_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Slug = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    WikiCategoryName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ObjectClass = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_categories", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scrape_errors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ScrapeLogId = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    PageTitle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    ItemName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    ErrorType = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    DetailsJson = table.Column<string>(type: "json", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrape_errors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scrape_errors_scrape_logs_ScrapeLogId",
                        column: x => x.ScrapeLogId,
                        principalTable: "scrape_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scrape_item_changes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ScrapeLogId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    ChangeType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CategorySlug = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    CategoryName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    ChangedFieldsJson = table.Column<string>(type: "json", nullable: true),
                    BeforeJson = table.Column<string>(type: "json", nullable: true),
                    AfterJson = table.Column<string>(type: "json", nullable: true),
                    ErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrape_item_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scrape_item_changes_scrape_logs_ScrapeLogId",
                        column: x => x.ScrapeLogId,
                        principalTable: "scrape_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_logs_StartedAt",
                table: "scrape_logs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_logs_Status",
                table: "scrape_logs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_items_CategoryId",
                table: "items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_Name",
                table: "item_categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_Slug",
                table: "item_categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_WikiCategoryName",
                table: "item_categories",
                column: "WikiCategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scrape_errors_OccurredAt",
                table: "scrape_errors",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_errors_Scope",
                table: "scrape_errors",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_errors_ScrapeLogId",
                table: "scrape_errors",
                column: "ScrapeLogId");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_item_changes_ChangeType",
                table: "scrape_item_changes",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_item_changes_ItemId",
                table: "scrape_item_changes",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_item_changes_OccurredAt",
                table: "scrape_item_changes",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_scrape_item_changes_ScrapeLogId",
                table: "scrape_item_changes",
                column: "ScrapeLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_items_item_categories_CategoryId",
                table: "items",
                column: "CategoryId",
                principalTable: "item_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_item_categories_CategoryId",
                table: "items");

            migrationBuilder.DropTable(
                name: "item_categories");

            migrationBuilder.DropTable(
                name: "scrape_errors");

            migrationBuilder.DropTable(
                name: "scrape_item_changes");

            migrationBuilder.DropIndex(
                name: "IX_scrape_logs_StartedAt",
                table: "scrape_logs");

            migrationBuilder.DropIndex(
                name: "IX_scrape_logs_Status",
                table: "scrape_logs");

            migrationBuilder.DropIndex(
                name: "IX_items_CategoryId",
                table: "items");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "CategorySlug",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "ErrorType",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "ItemsFailed",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "ItemsMissingFromSource",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "ItemsUnchanged",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "PagesDiscovered",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "PagesFailed",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "PagesProcessed",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "ScraperName",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "TriggeredBy",
                table: "scrape_logs");

            migrationBuilder.DropColumn(
                name: "AdditionalAttributesJson",
                table: "items");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "items");

            migrationBuilder.DropColumn(
                name: "IsMissingFromSource",
                table: "items");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "items");

            migrationBuilder.DropColumn(
                name: "MissingSince",
                table: "items");
        }
    }
}
