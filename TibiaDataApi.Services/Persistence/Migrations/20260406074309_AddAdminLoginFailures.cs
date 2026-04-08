using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddAdminLoginFailures : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_login_failures",
                columns: table => new
                {
                    IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    FirstFailedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastFailedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_login_failures", x => x.IpAddress);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_login_failures");
        }
    }
}
