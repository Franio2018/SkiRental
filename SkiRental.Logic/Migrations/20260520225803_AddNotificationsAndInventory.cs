using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkiRental.Logic.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RelatedRentalId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotifications", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Equipment",
                columns: new[] { "Id", "Brand", "Category", "CompatibleStyle", "DailyRate", "ImagePath", "LastMaintenanceDate", "MaintenanceNotes", "Model", "SerialNumber", "Size", "Status", "Stiffness", "TotalRentalDays", "YearOfProduction" },
                values: new object[,]
                {
                    { 6, "Salomon", "Boots", "Classic", 35m, null, null, null, "Prolink Access", "SAL-BT-40", 40m, "Available", null, 0, 2023 },
                    { 7, "Salomon", "Boots", "Classic", 35m, null, null, null, "Prolink Access", "SAL-BT-41", 41m, "Available", null, 0, 2023 },
                    { 8, "Salomon", "Boots", "Combi", 35m, null, null, null, "Prolink Access", "SAL-BT-42", 42m, "Available", null, 0, 2023 },
                    { 9, "Salomon", "Boots", "Combi", 35m, null, null, null, "Prolink Access", "SAL-BT-43", 43m, "Available", null, 0, 2024 },
                    { 10, "Fischer", "Boots", "Classic", 40m, null, null, null, "RC3 Classic", "FIS-BT-44", 44m, "Available", null, 0, 2024 },
                    { 11, "Fischer", "Boots", "Skate", 40m, null, null, null, "RC3 Skate", "FIS-BT-45", 45m, "Available", null, 0, 2024 },
                    { 12, "Leki", "Gloves", null, 10m, null, null, null, "Nordic Glove S", "GLO-S-001", 1m, "Available", null, 0, 2023 },
                    { 13, "Leki", "Gloves", null, 10m, null, null, null, "Nordic Glove M", "GLO-M-001", 2m, "Available", null, 0, 2023 },
                    { 14, "Uvex", "Helmet", null, 15m, null, null, null, "Sportstyle S", "HLM-S-001", 1m, "Available", null, 0, 2023 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_AppUserId",
                table: "AppNotifications",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_CreatedAt",
                table: "AppNotifications",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppNotifications");

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 14);
        }
    }
}
