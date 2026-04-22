using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkiRental.Logic.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    Stiffness = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CompatibleStyle = table.Column<string>(type: "TEXT", nullable: true),
                    YearOfProduction = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyRate = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    TotalRentalDays = table.Column<int>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaintenanceNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    ShoeSize = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillLevel = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredStyle = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rentals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualReturnTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    QrCode = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NfcTagData = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsPreauthorized = table.Column<bool>(type: "INTEGER", nullable: false),
                    PaymentStatus = table.Column<string>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PreauthorizedAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PayUOrderId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rentals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rentals_UserProfiles_CustomerProfileId",
                        column: x => x.CustomerProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RentalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProviderResponse = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RentalEquipments",
                columns: table => new
                {
                    RentalId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConditionAtPickup = table.Column<int>(type: "INTEGER", nullable: false),
                    ConditionAtReturn = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalEquipments", x => new { x.RentalId, x.EquipmentId });
                    table.ForeignKey(
                        name: "FK_RentalEquipments_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RentalEquipments_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Equipment",
                columns: new[] { "Id", "Brand", "Category", "CompatibleStyle", "DailyRate", "ImagePath", "LastMaintenanceDate", "MaintenanceNotes", "Model", "SerialNumber", "Size", "Status", "Stiffness", "TotalRentalDays", "YearOfProduction" },
                values: new object[,]
                {
                    { 1, "Fischer", "Skis", "Classic", 60m, null, null, null, "Twin Skin Pro", "FISCH-CL-001", 192m, "Available", 52, 0, 2023 },
                    { 2, "Fischer", "Skis", "Classic", 60m, null, null, null, "Twin Skin Pro", "FISCH-CL-002", 197m, "Available", 62, 0, 2023 },
                    { 3, "Salomon", "Skis", "Skate", 70m, null, null, null, "S/Race Skate Plus", "SALOMON-SK-001", 182m, "Available", 62, 0, 2024 },
                    { 4, "Leki", "Poles", "Classic", 20m, null, null, null, "Tour Stick Vario", "LEKI-CL-001", 150m, "Available", null, 0, 2023 },
                    { 5, "Leki", "Poles", "Skate", 20m, null, null, null, "CC 450 Vario", "LEKI-SK-001", 155m, "Available", null, 0, 2023 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_SerialNumber",
                table: "Equipment",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RentalId",
                table: "Payments",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalEquipments_EquipmentId",
                table: "RentalEquipments",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_CustomerProfileId",
                table: "Rentals",
                column: "CustomerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_QrCode",
                table: "Rentals",
                column: "QrCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RentalEquipments");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Rentals");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
