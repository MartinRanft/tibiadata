using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddGemModifierVocationIndex : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_gem_modifiers_VocationRestriction",
                table: "gem_modifiers",
                column: "VocationRestriction");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_gem_modifiers_VocationRestriction",
                table: "gem_modifiers");
        }
    }
}
