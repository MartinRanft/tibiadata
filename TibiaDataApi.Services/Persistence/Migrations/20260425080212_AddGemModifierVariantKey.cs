using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddGemModifierVariantKey : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_gem_modifiers_Name_ModifierType",
                table: "gem_modifiers");

            migrationBuilder.DropIndex(
                name: "IX_gem_modifiers_VocationRestriction",
                table: "gem_modifiers");

            migrationBuilder.AddColumn<string>(
                name: "VariantKey",
                table: "gem_modifiers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_gem_modifiers_ModifierType_VariantKey",
                table: "gem_modifiers",
                columns: new[] { "ModifierType", "VariantKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gem_modifiers_Name_ModifierType_VocationRestriction",
                table: "gem_modifiers",
                columns: new[] { "Name", "ModifierType", "VocationRestriction" });
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_gem_modifiers_ModifierType_VariantKey",
                table: "gem_modifiers");

            migrationBuilder.DropIndex(
                name: "IX_gem_modifiers_Name_ModifierType_VocationRestriction",
                table: "gem_modifiers");

            migrationBuilder.DropColumn(
                name: "VariantKey",
                table: "gem_modifiers");

            migrationBuilder.CreateIndex(
                name: "IX_gem_modifiers_Name_ModifierType",
                table: "gem_modifiers",
                columns: new[] { "Name", "ModifierType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gem_modifiers_VocationRestriction",
                table: "gem_modifiers",
                column: "VocationRestriction");
        }
    }
}
