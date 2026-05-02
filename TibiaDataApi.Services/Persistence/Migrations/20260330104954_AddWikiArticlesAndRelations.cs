using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddWikiArticlesAndRelations : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wiki_articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ContentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Summary = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: true),
                    PlainTextContent = table.Column<string>(type: "longtext", nullable: true),
                    RawWikiText = table.Column<string>(type: "longtext", nullable: true),
                    InfoboxTemplate = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    InfoboxJson = table.Column<string>(type: "json", nullable: true),
                    Sections = table.Column<string>(type: "json", nullable: false),
                    LinkedTitles = table.Column<string>(type: "json", nullable: false),
                    AdditionalAttributesJson = table.Column<string>(type: "json", nullable: true),
                    WikiUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsMissingFromSource = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MissingSince = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wiki_articles", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wiki_article_categories",
                columns: table => new
                {
                    WikiArticleId = table.Column<int>(type: "int", nullable: false),
                    WikiCategoryId = table.Column<int>(type: "int", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsMissingFromSource = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MissingSince = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wiki_article_categories", x => new { x.WikiArticleId, x.WikiCategoryId });
                    table.ForeignKey(
                        name: "FK_wiki_article_categories_wiki_articles_WikiArticleId",
                        column: x => x.WikiArticleId,
                        principalTable: "wiki_articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_wiki_article_categories_wiki_categories_WikiCategoryId",
                        column: x => x.WikiCategoryId,
                        principalTable: "wiki_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_article_categories_IsMissingFromSource",
                table: "wiki_article_categories",
                column: "IsMissingFromSource");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_article_categories_LastSeenAt",
                table: "wiki_article_categories",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_article_categories_WikiCategoryId",
                table: "wiki_article_categories",
                column: "WikiCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_articles_ContentType_Title",
                table: "wiki_articles",
                columns: new[] { "ContentType", "Title" },
                unique: true);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wiki_article_categories");

            migrationBuilder.DropTable(
                name: "wiki_articles");
        }
    }
}
