using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddCreatureInfoboxJson : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InfoboxJson",
                table: "creatures",
                type: "json",
                nullable: true);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InfoboxJson",
                table: "creatures");
        }
    }
}
