using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddCreatureBestiaryJson : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BestiaryJson",
                table: "creatures",
                type: "json",
                nullable: true);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BestiaryJson",
                table: "creatures");
        }
    }
}
