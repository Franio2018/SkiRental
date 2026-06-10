using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkiRental.Logic.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGlovesAndHelmetEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM RentalEquipments
                WHERE EquipmentId IN (SELECT Id FROM Equipment WHERE Category IN ('Gloves', 'Helmet'));
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM Equipment WHERE Category IN ('Gloves', 'Helmet');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Equipment",
                columns: new[] { "Id", "Brand", "Category", "CompatibleStyle", "DailyRate", "ImagePath", "LastMaintenanceDate", "MaintenanceNotes", "Model", "SerialNumber", "Size", "Status", "Stiffness", "TotalRentalDays", "YearOfProduction" },
                values: new object[,]
                {
                    { 12, "Leki", "Gloves", null, 10m, null, null, null, "Nordic Glove S", "GLO-S-001", 1m, "Available", null, 0, 2023 },
                    { 13, "Leki", "Gloves", null, 10m, null, null, null, "Nordic Glove M", "GLO-M-001", 2m, "Available", null, 0, 2023 },
                    { 14, "Uvex", "Helmet", null, 15m, null, null, null, "Sportstyle S", "HLM-S-001", 1m, "Available", null, 0, 2023 }
                });
        }
    }
}
