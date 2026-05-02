using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddScraperExecutionLease : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scraper_execution_leases",
                columns: table => new
                {
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    OwnerId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scraper_execution_leases", x => x.Name);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_scraper_execution_leases_ExpiresAt",
                table: "scraper_execution_leases",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_scraper_execution_leases_UpdatedAt",
                table: "scraper_execution_leases",
                column: "UpdatedAt");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scraper_execution_leases");
        }
    }
}
