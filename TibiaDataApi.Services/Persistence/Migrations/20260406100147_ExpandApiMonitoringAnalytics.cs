using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class ExpandApiMonitoringAnalytics : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CacheStatus",
                table: "api_request_logs",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "ResponseSizeBytes",
                table: "api_request_logs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "api_request_logs",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "api_request_daily_aggregates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    DayUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    BlockedCount = table.Column<int>(type: "int", nullable: false),
                    CacheHitCount = table.Column<int>(type: "int", nullable: false),
                    CacheMissCount = table.Column<int>(type: "int", nullable: false),
                    CacheBypassCount = table.Column<int>(type: "int", nullable: false),
                    TotalResponseSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    TotalDurationMs = table.Column<double>(type: "double", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_request_daily_aggregates", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_CacheStatus",
                table: "api_request_logs",
                column: "CacheStatus");

            migrationBuilder.CreateIndex(
                name: "IX_api_request_daily_aggregates_DayUtc",
                table: "api_request_daily_aggregates",
                column: "DayUtc",
                unique: true);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_request_daily_aggregates");

            migrationBuilder.DropIndex(
                name: "IX_api_request_logs_CacheStatus",
                table: "api_request_logs");

            migrationBuilder.DropColumn(
                name: "CacheStatus",
                table: "api_request_logs");

            migrationBuilder.DropColumn(
                name: "ResponseSizeBytes",
                table: "api_request_logs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "api_request_logs");
        }
    }
}
