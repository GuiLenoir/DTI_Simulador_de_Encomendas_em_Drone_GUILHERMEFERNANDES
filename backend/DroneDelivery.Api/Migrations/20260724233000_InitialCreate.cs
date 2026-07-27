using DroneDelivery.Api.Data;
using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneDelivery.Api.Migrations;

[DbContext(typeof(DroneDeliveryDbContext))]
[Migration("20260724233000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "Drones",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                MaxPackageWeightKg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                MaxRangeKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                BatteryLevelPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                CurrentX = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                CurrentY = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Drones", x => x.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                CustomerName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                DestinationX = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                DestinationY = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                PackageWeightKg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "Deliveries",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                DroneId = table.Column<int>(type: "int", nullable: false),
                OrderId = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                StartX = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                StartY = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                DestinationX = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                DestinationY = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                EndX = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                EndY = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                EstimatedDistanceKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                EstimatedBatteryConsumptionPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                EstimatedDurationMinutes = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                AllocatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                DeliveredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Deliveries", x => x.Id);
                table.ForeignKey("FK_Deliveries_Drones_DroneId", x => x.DroneId, "Drones", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Deliveries_Orders_OrderId", x => x.OrderId, "Orders", "Id", onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        migrationBuilder.InsertData(
            table: "Drones",
            columns: new[] { "Id", "BatteryLevelPercent", "Code", "CreatedAt", "CurrentX", "CurrentY", "MaxPackageWeightKg", "MaxRangeKm", "Status", "UpdatedAt" },
            columnTypes: new[] { "int", "decimal(5,2)", "varchar(50)", "datetime(6)", "decimal(10,2)", "decimal(10,2)", "decimal(10,2)", "decimal(10,2)", "varchar(20)", "datetime(6)" },
            values: new object[,]
            {
                { 1, 100m, "DRN-001", seedTime, 0m, 0m, 2.5m, 20m, "Idle", seedTime },
                { 2, 85m, "DRN-002", seedTime, 2m, 1m, 5m, 35m, "Idle", seedTime },
                { 3, 70m, "DRN-003", seedTime, -3m, 4m, 10m, 60m, "Idle", seedTime }
            });

        migrationBuilder.CreateIndex(name: "IX_Deliveries_DroneId", table: "Deliveries", column: "DroneId");
        migrationBuilder.CreateIndex(name: "IX_Deliveries_OrderId", table: "Deliveries", column: "OrderId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Drones_Code", table: "Drones", column: "Code", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Deliveries");
        migrationBuilder.DropTable(name: "Drones");
        migrationBuilder.DropTable(name: "Orders");
    }
}
