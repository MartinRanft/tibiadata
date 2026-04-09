using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class SeparateItemImageSyncQueue : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_image_sync_queue",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    WikiPageTitle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastAttemptedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastCompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_image_sync_queue", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_item_image_sync_queue_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_item_image_sync_queue_RequestedAt",
                table: "item_image_sync_queue",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_item_image_sync_queue_Status",
                table: "item_image_sync_queue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_item_image_sync_queue_UpdatedAt",
                table: "item_image_sync_queue",
                column: "UpdatedAt");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_image_sync_queue");
        }
    }
}
