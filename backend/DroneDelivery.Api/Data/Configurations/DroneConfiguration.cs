using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneDelivery.Api.Data.Configurations;

public sealed class DroneConfiguration : IEntityTypeConfiguration<Drone>
{
    public void Configure(EntityTypeBuilder<Drone> builder)
    {
        builder.ToTable("Drones");
        builder.HasKey(drone => drone.Id);
        builder.Property(drone => drone.Code).HasMaxLength(50).IsRequired();
        builder.Property(drone => drone.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(drone => drone.Code).IsUnique();
        builder.Property(drone => drone.MaxPackageWeightKg).HasPrecision(10, 2);
        builder.Property(drone => drone.MaxRangeKm).HasPrecision(10, 2);
        builder.Property(drone => drone.BatteryLevelPercent).HasPrecision(5, 2);
        builder.Property(drone => drone.BatterySafetyMarginPercentagePoints).HasPrecision(5, 2);
        builder.Property(drone => drone.BatteryAtChargingStartPercentage).HasPrecision(5, 2);
        builder.Property(drone => drone.ChargingRatePercentagePointsPerSecond).HasPrecision(5, 2);
        builder.Property(drone => drone.CurrentX).HasPrecision(10, 2);
        builder.Property(drone => drone.CurrentY).HasPrecision(10, 2);
        builder.Property(drone => drone.AverageSpeedKmPerHour).HasPrecision(10, 2);
        builder.Property(drone => drone.BatteryConsumptionPercentagePerKm).HasPrecision(5, 2);
        builder.Property(drone => drone.Notes).HasMaxLength(500);
        builder.Property(drone => drone.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(drone => drone.Status);
        builder.HasIndex(drone => drone.IsActive);

        var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Drone { Id = 1, Code = "DRN-001", Name = "Drone leve", MaxPackageWeightKg = 2.5m, MaxRangeKm = 20m, BatteryLevelPercent = 100m, BatterySafetyMarginPercentagePoints = 5m, ChargingRatePercentagePointsPerSecond = 2m, CurrentX = 0m, CurrentY = 0m, AverageSpeedKmPerHour = 60m, BatteryConsumptionPercentagePerKm = 1.5m, IsActive = true, Status = DroneStatus.Idle, CreatedAt = seedTime, UpdatedAt = seedTime },
            new Drone { Id = 2, Code = "DRN-002", Name = "Drone medio", MaxPackageWeightKg = 5m, MaxRangeKm = 35m, BatteryLevelPercent = 85m, BatterySafetyMarginPercentagePoints = 5m, ChargingRatePercentagePointsPerSecond = 2m, CurrentX = 2m, CurrentY = 1m, AverageSpeedKmPerHour = 60m, BatteryConsumptionPercentagePerKm = 1.5m, IsActive = true, Status = DroneStatus.Idle, CreatedAt = seedTime, UpdatedAt = seedTime },
            new Drone { Id = 3, Code = "DRN-003", Name = "Drone pesado", MaxPackageWeightKg = 10m, MaxRangeKm = 60m, BatteryLevelPercent = 70m, BatterySafetyMarginPercentagePoints = 5m, ChargingRatePercentagePointsPerSecond = 2m, CurrentX = -3m, CurrentY = 4m, AverageSpeedKmPerHour = 60m, BatteryConsumptionPercentagePerKm = 1.5m, IsActive = true, Status = DroneStatus.Idle, CreatedAt = seedTime, UpdatedAt = seedTime });
    }
}
