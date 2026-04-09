using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddScheduledScraperConfigurations : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scheduled_scraper_configurations",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScheduleHour = table.Column<int>(type: "int", nullable: false),
                    ScheduleMinute = table.Column<int>(type: "int", nullable: false),
                    LastTriggeredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_scraper_configurations", x => x.Key);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_scraper_configurations");
        }
    }
}
