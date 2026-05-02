using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddGemModCatalog : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gem_modifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    WikiUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    ModifierType = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    VocationRestriction = table.Column<int>(type: "int", nullable: true),
                    IsComboMod = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HasTradeoff = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gem_modifiers", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "gems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    WikiUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    GemFamily = table.Column<int>(type: "int", nullable: false),
                    GemSize = table.Column<int>(type: "int", nullable: false),
                    VocationRestriction = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gems", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "gem_modifier_grades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GemModifierId = table.Column<int>(type: "int", nullable: false),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    ValueText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    ValueNumeric = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    IsIncomplete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gem_modifier_grades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gem_modifier_grades_gem_modifiers_GemModifierId",
                        column: x => x.GemModifierId,
                        principalTable: "gem_modifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_gem_modifier_grades_GemModifierId_Grade",
                table: "gem_modifier_grades",
                columns: new[] { "GemModifierId", "Grade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gem_modifiers_ModifierType_Category",
                table: "gem_modifiers",
                columns: new[] { "ModifierType", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_gem_modifiers_Name_ModifierType",
                table: "gem_modifiers",
                columns: new[] { "Name", "ModifierType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gem_modifiers_VocationRestriction",
                table: "gem_modifiers",
                column: "VocationRestriction");

            migrationBuilder.CreateIndex(
                name: "IX_gems_GemFamily_GemSize",
                table: "gems",
                columns: new[] { "GemFamily", "GemSize" });

            migrationBuilder.CreateIndex(
                name: "IX_gems_Name",
                table: "gems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gems_VocationRestriction",
                table: "gems",
                column: "VocationRestriction");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gem_modifier_grades");

            migrationBuilder.DropTable(
                name: "gems");

            migrationBuilder.DropTable(
                name: "gem_modifiers");
        }
    }
}
