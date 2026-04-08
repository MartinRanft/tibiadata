using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddBackgroundJobExecutions : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "background_job_executions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    JobName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    TriggeredBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    LeaseName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    LeaseOwnerId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    ProcessedCount = table.Column<int>(type: "int", nullable: false),
                    SucceededCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    MetadataJson = table.Column<string>(type: "json", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DurationMs = table.Column<double>(type: "double", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_background_job_executions", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_background_job_executions_JobName",
                table: "background_job_executions",
                column: "JobName");

            migrationBuilder.CreateIndex(
                name: "IX_background_job_executions_JobName_StartedAt",
                table: "background_job_executions",
                columns: new[] { "JobName", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_background_job_executions_StartedAt",
                table: "background_job_executions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_background_job_executions_Status",
                table: "background_job_executions",
                column: "Status");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "background_job_executions");
        }
    }
}
