using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneDelivery.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDroneCrudAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageSpeedKmPerHour",
                table: "Drones",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 60m);

            migrationBuilder.AddColumn<decimal>(
                name: "BatteryConsumptionPercentagePerKm",
                table: "Drones",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 1.5m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Drones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Drones",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Drones",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE `Drones` SET `Name` = `Code` WHERE `Name` = '';");

            migrationBuilder.CreateTable(
                name: "DroneSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BatterySafetyMarginPercentagePoints = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DroneSettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "DroneSettings",
                columns: new[] { "Id", "BatterySafetyMarginPercentagePoints", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[] { 1, 5m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Drones",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AverageSpeedKmPerHour", "BatteryConsumptionPercentagePerKm", "IsActive", "Name", "Notes" },
                values: new object[] { 60m, 1.5m, true, "Drone leve", null });

            migrationBuilder.UpdateData(
                table: "Drones",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AverageSpeedKmPerHour", "BatteryConsumptionPercentagePerKm", "IsActive", "Name", "Notes" },
                values: new object[] { 60m, 1.5m, true, "Drone medio", null });

            migrationBuilder.UpdateData(
                table: "Drones",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AverageSpeedKmPerHour", "BatteryConsumptionPercentagePerKm", "IsActive", "Name", "Notes" },
                values: new object[] { 60m, 1.5m, true, "Drone pesado", null });

            migrationBuilder.CreateIndex(
                name: "IX_Drones_IsActive",
                table: "Drones",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DroneSettings");

            migrationBuilder.DropIndex(
                name: "IX_Drones_IsActive",
                table: "Drones");

            migrationBuilder.DropColumn(name: "AverageSpeedKmPerHour", table: "Drones");
            migrationBuilder.DropColumn(name: "BatteryConsumptionPercentagePerKm", table: "Drones");
            migrationBuilder.DropColumn(name: "IsActive", table: "Drones");
            migrationBuilder.DropColumn(name: "Name", table: "Drones");
            migrationBuilder.DropColumn(name: "Notes", table: "Drones");
        }
    }
}
