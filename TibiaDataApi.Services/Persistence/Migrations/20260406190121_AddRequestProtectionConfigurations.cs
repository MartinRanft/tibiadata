using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddRequestProtectionConfigurations : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "request_protection_configurations",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PublicApiTokenLimit = table.Column<int>(type: "int", nullable: false),
                    PublicApiTokensPerPeriod = table.Column<int>(type: "int", nullable: false),
                    PublicApiReplenishmentSeconds = table.Column<int>(type: "int", nullable: false),
                    PublicApiTokenQueueLimit = table.Column<int>(type: "int", nullable: false),
                    PublicApiConcurrentPermitLimit = table.Column<int>(type: "int", nullable: false),
                    PublicApiConcurrentQueueLimit = table.Column<int>(type: "int", nullable: false),
                    AdminReadApiTokenLimit = table.Column<int>(type: "int", nullable: false),
                    AdminReadApiTokensPerPeriod = table.Column<int>(type: "int", nullable: false),
                    AdminReadApiReplenishmentSeconds = table.Column<int>(type: "int", nullable: false),
                    AdminReadApiTokenQueueLimit = table.Column<int>(type: "int", nullable: false),
                    AdminReadApiConcurrentPermitLimit = table.Column<int>(type: "int", nullable: false),
                    AdminReadApiConcurrentQueueLimit = table.Column<int>(type: "int", nullable: false),
                    AdminMutationApiTokenLimit = table.Column<int>(type: "int", nullable: false),
                    AdminMutationApiTokensPerPeriod = table.Column<int>(type: "int", nullable: false),
                    AdminMutationApiReplenishmentSeconds = table.Column<int>(type: "int", nullable: false),
                    AdminMutationApiTokenQueueLimit = table.Column<int>(type: "int", nullable: false),
                    AdminMutationApiConcurrentPermitLimit = table.Column<int>(type: "int", nullable: false),
                    AdminMutationApiConcurrentQueueLimit = table.Column<int>(type: "int", nullable: false),
                    AdminLoginTokenLimit = table.Column<int>(type: "int", nullable: false),
                    AdminLoginTokensPerPeriod = table.Column<int>(type: "int", nullable: false),
                    AdminLoginReplenishmentSeconds = table.Column<int>(type: "int", nullable: false),
                    AdminLoginTokenQueueLimit = table.Column<int>(type: "int", nullable: false),
                    AdminLoginConcurrentPermitLimit = table.Column<int>(type: "int", nullable: false),
                    AdminLoginConcurrentQueueLimit = table.Column<int>(type: "int", nullable: false),
                    HealthApiTokenLimit = table.Column<int>(type: "int", nullable: false),
                    HealthApiTokensPerPeriod = table.Column<int>(type: "int", nullable: false),
                    HealthApiReplenishmentSeconds = table.Column<int>(type: "int", nullable: false),
                    HealthApiTokenQueueLimit = table.Column<int>(type: "int", nullable: false),
                    HealthApiConcurrentPermitLimit = table.Column<int>(type: "int", nullable: false),
                    HealthApiConcurrentQueueLimit = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_protection_configurations", x => x.Key);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_protection_configurations");
        }
    }
}
