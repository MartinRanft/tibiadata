using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddNormalizedEntityNames : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedTitle",
                table: "wiki_articles",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedActualName",
                table: "items",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "items",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "creatures",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE wiki_articles
                SET NormalizedTitle = LOWER(TRIM(REPLACE(Title, '_', ' ')))
                WHERE NormalizedTitle IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE items
                SET
                    NormalizedName = LOWER(TRIM(REPLACE(Name, '_', ' '))),
                    NormalizedActualName = NULLIF(LOWER(TRIM(REPLACE(ActualName, '_', ' '))), '')
                WHERE NormalizedName IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE creatures
                SET NormalizedName = LOWER(TRIM(REPLACE(Name, '_', ' ')))
                WHERE NormalizedName IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedTitle",
                table: "wiki_articles",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "items",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "creatures",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_wiki_articles_ContentType_NormalizedTitle",
                table: "wiki_articles",
                columns: new[] { "ContentType", "NormalizedTitle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_NormalizedActualName",
                table: "items",
                column: "NormalizedActualName");

            migrationBuilder.CreateIndex(
                name: "IX_items_NormalizedName",
                table: "items",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_creatures_NormalizedName",
                table: "creatures",
                column: "NormalizedName",
                unique: true);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wiki_articles_ContentType_NormalizedTitle",
                table: "wiki_articles");

            migrationBuilder.DropIndex(
                name: "IX_items_NormalizedActualName",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_NormalizedName",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_creatures_NormalizedName",
                table: "creatures");

            migrationBuilder.DropColumn(
                name: "NormalizedTitle",
                table: "wiki_articles");

            migrationBuilder.DropColumn(
                name: "NormalizedActualName",
                table: "items");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "items");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "creatures");
        }
    }
}
