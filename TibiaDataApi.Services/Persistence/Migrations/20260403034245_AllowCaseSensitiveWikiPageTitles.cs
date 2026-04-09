using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AllowCaseSensitiveWikiPageTitles : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wiki_articles_ContentType_Title",
                table: "wiki_articles");

            migrationBuilder.Sql(
                """
                UPDATE wiki_articles
                SET NormalizedTitle = CONCAT('wiki-page:', UPPER(SHA2(Title, 256)))
                WHERE ContentType = 'WikiPage';
                """);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE wiki_articles
                SET NormalizedTitle = LOWER(TRIM(REPLACE(Title, '_', ' ')))
                WHERE ContentType = 'WikiPage';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_wiki_articles_ContentType_Title",
                table: "wiki_articles",
                columns: new[] { "ContentType", "Title" },
                unique: true);
        }
    }
}
