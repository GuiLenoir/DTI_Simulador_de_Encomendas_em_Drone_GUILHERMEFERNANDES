using System;
using DroneDelivery.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneDelivery.Api.Migrations;

[DbContext(typeof(DroneDeliveryDbContext))]
[Migration("20260725043000_AddDeliveryTimeline")]
public partial class AddDeliveryTimeline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var defaultUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAtUtc",
            table: "Deliveries",
            type: "datetime(6)",
            nullable: false,
            defaultValue: defaultUtc);

        migrationBuilder.AddColumn<DateTime>(
            name: "LoadingStartedAtUtc",
            table: "Deliveries",
            type: "datetime(6)",
            nullable: false,
            defaultValue: defaultUtc);

        migrationBuilder.AddColumn<DateTime>(
            name: "FlyingStartedAtUtc",
            table: "Deliveries",
            type: "datetime(6)",
            nullable: false,
            defaultValue: defaultUtc);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeliveringStartedAtUtc",
            table: "Deliveries",
            type: "datetime(6)",
            nullable: false,
            defaultValue: defaultUtc);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReturningStartedAtUtc",
            table: "Deliveries",
            type: "datetime(6)",
            nullable: false,
            defaultValue: defaultUtc);

        migrationBuilder.AddColumn<DateTime>(
            name: "CompletedAtUtc",
            table: "Deliveries",
            type: "datetime(6)",
            nullable: false,
            defaultValue: defaultUtc);

        migrationBuilder.AddColumn<int>(
            name: "LoadingDurationSeconds",
            table: "Deliveries",
            type: "int",
            nullable: false,
            defaultValue: 3);

        migrationBuilder.AddColumn<int>(
            name: "OutboundFlightDurationSeconds",
            table: "Deliveries",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "DeliveryDurationSeconds",
            table: "Deliveries",
            type: "int",
            nullable: false,
            defaultValue: 3);

        migrationBuilder.AddColumn<int>(
            name: "ReturnFlightDurationSeconds",
            table: "Deliveries",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.Sql("""
            UPDATE `Deliveries`
            SET
                `CreatedAtUtc` = `AllocatedAt`,
                `LoadingStartedAtUtc` = `AllocatedAt`,
                `FlyingStartedAtUtc` = DATE_ADD(`AllocatedAt`, INTERVAL 3 SECOND),
                `DeliveringStartedAtUtc` = DATE_ADD(`AllocatedAt`, INTERVAL 4 SECOND),
                `ReturningStartedAtUtc` = DATE_ADD(`AllocatedAt`, INTERVAL 7 SECOND),
                `CompletedAtUtc` = COALESCE(`DeliveredAt`, DATE_ADD(`AllocatedAt`, INTERVAL 8 SECOND))
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CreatedAtUtc", table: "Deliveries");
        migrationBuilder.DropColumn(name: "LoadingStartedAtUtc", table: "Deliveries");
        migrationBuilder.DropColumn(name: "FlyingStartedAtUtc", table: "Deliveries");
        migrationBuilder.DropColumn(name: "DeliveringStartedAtUtc", table: "Deliveries");
        migrationBuilder.DropColumn(name: "ReturningStartedAtUtc", table: "Deliveries");
        migrationBuilder.DropColumn(name: "CompletedAtUtc", table: "Deliveries");
        migrationBuilder.DropColumn(name: "LoadingDurationSeconds", table: "Deliveries");
        migrationBuilder.DropColumn(name: "OutboundFlightDurationSeconds", table: "Deliveries");
        migrationBuilder.DropColumn(name: "DeliveryDurationSeconds", table: "Deliveries");
        migrationBuilder.DropColumn(name: "ReturnFlightDurationSeconds", table: "Deliveries");
    }
}
