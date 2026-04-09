using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddApiStatsAndIpBans : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_request_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Method = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Route = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<double>(type: "double", nullable: false),
                    IsBlocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_request_logs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ip_bans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RevokedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    RevocationReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ip_bans", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_IpAddress",
                table: "api_request_logs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_IsBlocked",
                table: "api_request_logs",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_Method_Route",
                table: "api_request_logs",
                columns: new[] { "Method", "Route" });

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_OccurredAt",
                table: "api_request_logs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ip_bans_IpAddress",
                table: "ip_bans",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ip_bans_IpAddress_IsActive",
                table: "ip_bans",
                columns: new[] { "IpAddress", "IsActive" });
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_request_logs");

            migrationBuilder.DropTable(
                name: "ip_bans");
        }
    }
}
