using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class GeneralizeWikiCategories : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_item_categories_CategoryId",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_item_categories_Name",
                table: "item_categories");

            migrationBuilder.DropIndex(
                name: "IX_item_categories_WikiCategoryName",
                table: "item_categories");

            migrationBuilder.RenameTable(
                name: "item_categories",
                newName: "wiki_categories");

            migrationBuilder.RenameIndex(
                name: "IX_item_categories_Slug",
                table: "wiki_categories",
                newName: "IX_wiki_categories_Slug");

            migrationBuilder.RenameColumn(
                name: "WikiCategoryName",
                table: "wiki_categories",
                newName: "SourceTitle");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "wiki_categories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Item")
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "wiki_categories",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Unassigned")
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GroupSlug",
                table: "wiki_categories",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "unassigned")
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SourceKind",
                table: "wiki_categories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "CategoryMembers")
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SourceSection",
                table: "wiki_categories",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "wiki_categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_wiki_categories_ContentType_GroupSlug",
                table: "wiki_categories",
                columns: new[] { "ContentType", "GroupSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_wiki_categories_ContentType_SortOrder",
                table: "wiki_categories",
                columns: new[] { "ContentType", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_items_wiki_categories_CategoryId",
                table: "items",
                column: "CategoryId",
                principalTable: "wiki_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_wiki_categories_CategoryId",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_wiki_categories_ContentType_GroupSlug",
                table: "wiki_categories");

            migrationBuilder.DropIndex(
                name: "IX_wiki_categories_ContentType_SortOrder",
                table: "wiki_categories");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "wiki_categories");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "wiki_categories");

            migrationBuilder.DropColumn(
                name: "GroupSlug",
                table: "wiki_categories");

            migrationBuilder.DropColumn(
                name: "SourceKind",
                table: "wiki_categories");

            migrationBuilder.DropColumn(
                name: "SourceSection",
                table: "wiki_categories");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "wiki_categories");

            migrationBuilder.RenameColumn(
                name: "SourceTitle",
                table: "wiki_categories",
                newName: "WikiCategoryName");

            migrationBuilder.RenameTable(
                name: "wiki_categories",
                newName: "item_categories");

            migrationBuilder.RenameIndex(
                name: "IX_wiki_categories_Slug",
                table: "item_categories",
                newName: "IX_item_categories_Slug");

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_Name",
                table: "item_categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_WikiCategoryName",
                table: "item_categories",
                column: "WikiCategoryName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_items_item_categories_CategoryId",
                table: "items",
                column: "CategoryId",
                principalTable: "item_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
