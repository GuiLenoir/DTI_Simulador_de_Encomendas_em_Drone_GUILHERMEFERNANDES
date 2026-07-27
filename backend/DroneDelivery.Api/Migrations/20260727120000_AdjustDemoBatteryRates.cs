using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneDelivery.Api.Migrations;

public partial class AdjustDemoBatteryRates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "Drones",
            keyColumn: "Id",
            keyValue: 1,
            column: "BatteryConsumptionPercentagePerKm",
            value: 2.5m);

        migrationBuilder.UpdateData(
            table: "Drones",
            keyColumn: "Id",
            keyValue: 2,
            column: "BatteryConsumptionPercentagePerKm",
            value: 2.5m);

        migrationBuilder.UpdateData(
            table: "Drones",
            keyColumn: "Id",
            keyValue: 3,
            column: "BatteryConsumptionPercentagePerKm",
            value: 2.5m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "Drones",
            keyColumn: "Id",
            keyValue: 1,
            column: "BatteryConsumptionPercentagePerKm",
            value: 1.5m);

        migrationBuilder.UpdateData(
            table: "Drones",
            keyColumn: "Id",
            keyValue: 2,
            column: "BatteryConsumptionPercentagePerKm",
            value: 1.5m);

        migrationBuilder.UpdateData(
            table: "Drones",
            keyColumn: "Id",
            keyValue: 3,
            column: "BatteryConsumptionPercentagePerKm",
            value: 1.5m);
    }
}
