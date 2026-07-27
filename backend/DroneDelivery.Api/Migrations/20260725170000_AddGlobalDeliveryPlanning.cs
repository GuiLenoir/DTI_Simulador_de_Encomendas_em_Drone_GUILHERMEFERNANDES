using System;
using DroneDelivery.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneDelivery.Api.Migrations;

[DbContext(typeof(DroneDeliveryDbContext))]
[Migration("20260725170000_AddGlobalDeliveryPlanning")]
public partial class AddGlobalDeliveryPlanning : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "QueueStatus",
            table: "Orders",
            type: "varchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "NotQueued")
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<DateTime>(
            name: "QueuedAtUtc",
            table: "Orders",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "BatterySafetyMarginPercentagePoints",
            table: "Drones",
            type: "decimal(5,2)",
            precision: 5,
            scale: 2,
            nullable: false,
            defaultValue: 5m);

        migrationBuilder.AddColumn<DateTime>(
            name: "ChargingStartedAtUtc",
            table: "Drones",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "BatteryAtChargingStartPercentage",
            table: "Drones",
            type: "decimal(5,2)",
            precision: 5,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ChargingRatePercentagePointsPerSecond",
            table: "Drones",
            type: "decimal(5,2)",
            precision: 5,
            scale: 2,
            nullable: false,
            defaultValue: 2m);

        migrationBuilder.AddColumn<DateTime>(
            name: "ChargingCompletedAtUtc",
            table: "Drones",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Trips",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                DroneId = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                PlannedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LoadingStartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                FlyingStartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                DeliveringStartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ReturningStartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                TotalWeightKg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                EstimatedDistanceKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                EstimatedBatteryConsumptionPercentagePoints = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                BatterySafetyMarginPercentagePoints = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                MinimumRequiredBatteryPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                BatteryAtDeparturePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                ExpectedBatteryAtReturnPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                LoadingDurationSeconds = table.Column<int>(type: "int", nullable: false),
                OutboundFlightDurationSeconds = table.Column<int>(type: "int", nullable: false),
                DeliveryDurationSeconds = table.Column<int>(type: "int", nullable: false),
                ReturnFlightDurationSeconds = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Trips", x => x.Id);
                table.ForeignKey(
                    name: "FK_Trips_Drones_DroneId",
                    column: x => x.DroneId,
                    principalTable: "Drones",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "TripOrders",
            columns: table => new
            {
                TripId = table.Column<int>(type: "int", nullable: false),
                OrderId = table.Column<int>(type: "int", nullable: false),
                DeliverySequence = table.Column<int>(type: "int", nullable: false),
                EstimatedArrivalAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                DeliveryStartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                DeliveryCompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TripOrders", x => new { x.TripId, x.OrderId });
                table.ForeignKey(
                    name: "FK_TripOrders_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TripOrders_Trips_TripId",
                    column: x => x.TripId,
                    principalTable: "Trips",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(name: "IX_Orders_QueueStatus", table: "Orders", column: "QueueStatus");
        migrationBuilder.CreateIndex(name: "IX_Orders_QueuedAtUtc", table: "Orders", column: "QueuedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_Drones_Status", table: "Drones", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_Trips_DroneId", table: "Trips", column: "DroneId");
        migrationBuilder.CreateIndex(name: "IX_Trips_Status", table: "Trips", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_TripOrders_OrderId", table: "TripOrders", column: "OrderId");
        migrationBuilder.CreateIndex(name: "IX_TripOrders_TripId_DeliverySequence", table: "TripOrders", columns: new[] { "TripId", "DeliverySequence" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TripOrders");
        migrationBuilder.DropTable(name: "Trips");
        migrationBuilder.DropIndex(name: "IX_Orders_QueueStatus", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Orders_QueuedAtUtc", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Drones_Status", table: "Drones");
        migrationBuilder.DropColumn(name: "QueueStatus", table: "Orders");
        migrationBuilder.DropColumn(name: "QueuedAtUtc", table: "Orders");
        migrationBuilder.DropColumn(name: "BatterySafetyMarginPercentagePoints", table: "Drones");
        migrationBuilder.DropColumn(name: "ChargingStartedAtUtc", table: "Drones");
        migrationBuilder.DropColumn(name: "BatteryAtChargingStartPercentage", table: "Drones");
        migrationBuilder.DropColumn(name: "ChargingRatePercentagePointsPerSecond", table: "Drones");
        migrationBuilder.DropColumn(name: "ChargingCompletedAtUtc", table: "Drones");
    }
}
