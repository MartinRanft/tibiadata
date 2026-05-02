using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TibiaDataApi.Services.Persistence.Migrations
{
        public partial class AddWheelPlannerLayout : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wheel_revelation_slots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Vocation = table.Column<int>(type: "int", nullable: false),
                    SlotKey = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    Quarter = table.Column<int>(type: "int", nullable: false),
                    RequiredPoints = table.Column<short>(type: "smallint", nullable: false),
                    WheelPerkId = table.Column<int>(type: "int", nullable: false),
                    WheelPerkOccurrenceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wheel_revelation_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wheel_revelation_slots_wheel_perk_occurrences_WheelPerkOccur~",
                        column: x => x.WheelPerkOccurrenceId,
                        principalTable: "wheel_perk_occurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_wheel_revelation_slots_wheel_perks_WheelPerkId",
                        column: x => x.WheelPerkId,
                        principalTable: "wheel_perks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wheel_sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Vocation = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Quarter = table.Column<int>(type: "int", nullable: false),
                    RadiusIndex = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    SectionPoints = table.Column<short>(type: "smallint", nullable: false),
                    ConvictionWheelPerkId = table.Column<int>(type: "int", nullable: false),
                    ConvictionWheelPerkOccurrenceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wheel_sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wheel_sections_wheel_perk_occurrences_ConvictionWheelPerkOcc~",
                        column: x => x.ConvictionWheelPerkOccurrenceId,
                        principalTable: "wheel_perk_occurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_wheel_sections_wheel_perks_ConvictionWheelPerkId",
                        column: x => x.ConvictionWheelPerkId,
                        principalTable: "wheel_perks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wheel_section_dedication_perks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    WheelSectionId = table.Column<int>(type: "int", nullable: false),
                    WheelPerkId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wheel_section_dedication_perks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wheel_section_dedication_perks_wheel_perks_WheelPerkId",
                        column: x => x.WheelPerkId,
                        principalTable: "wheel_perks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_wheel_section_dedication_perks_wheel_sections_WheelSectionId",
                        column: x => x.WheelSectionId,
                        principalTable: "wheel_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_wheel_revelation_slots_Vocation_SlotKey",
                table: "wheel_revelation_slots",
                columns: new[] { "Vocation", "SlotKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wheel_revelation_slots_WheelPerkId",
                table: "wheel_revelation_slots",
                column: "WheelPerkId");

            migrationBuilder.CreateIndex(
                name: "IX_wheel_revelation_slots_WheelPerkOccurrenceId",
                table: "wheel_revelation_slots",
                column: "WheelPerkOccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_wheel_section_dedication_perks_WheelPerkId",
                table: "wheel_section_dedication_perks",
                column: "WheelPerkId");

            migrationBuilder.CreateIndex(
                name: "IX_wheel_section_dedication_perks_WheelSectionId_SortOrder",
                table: "wheel_section_dedication_perks",
                columns: new[] { "WheelSectionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wheel_section_dedication_perks_WheelSectionId_WheelPerkId",
                table: "wheel_section_dedication_perks",
                columns: new[] { "WheelSectionId", "WheelPerkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wheel_sections_ConvictionWheelPerkId",
                table: "wheel_sections",
                column: "ConvictionWheelPerkId");

            migrationBuilder.CreateIndex(
                name: "IX_wheel_sections_ConvictionWheelPerkOccurrenceId",
                table: "wheel_sections",
                column: "ConvictionWheelPerkOccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_wheel_sections_Vocation_Quarter_SortOrder",
                table: "wheel_sections",
                columns: new[] { "Vocation", "Quarter", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wheel_sections_Vocation_SectionKey",
                table: "wheel_sections",
                columns: new[] { "Vocation", "SectionKey" },
                unique: true);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wheel_revelation_slots");

            migrationBuilder.DropTable(
                name: "wheel_section_dedication_perks");

            migrationBuilder.DropTable(
                name: "wheel_sections");
        }
    }
}
