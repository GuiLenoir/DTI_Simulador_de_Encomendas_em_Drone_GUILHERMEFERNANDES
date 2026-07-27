using System;
using DroneDelivery.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneDelivery.Api.Migrations;

[DbContext(typeof(DroneDeliveryDbContext))]
[Migration("20260725193000_AddNoFlyZones")]
public partial class AddNoFlyZones : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NoFlyZones",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NoFlyZones", x => x.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "NoFlyZonePoints",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                NoFlyZoneId = table.Column<int>(type: "int", nullable: false),
                Sequence = table.Column<int>(type: "int", nullable: false),
                X = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Y = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NoFlyZonePoints", x => x.Id);
                table.ForeignKey(
                    name: "FK_NoFlyZonePoints_NoFlyZones_NoFlyZoneId",
                    column: x => x.NoFlyZoneId,
                    principalTable: "NoFlyZones",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_NoFlyZonePoints_NoFlyZoneId_Sequence",
            table: "NoFlyZonePoints",
            columns: new[] { "NoFlyZoneId", "Sequence" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NoFlyZonePoints");
        migrationBuilder.DropTable(name: "NoFlyZones");
    }
}
