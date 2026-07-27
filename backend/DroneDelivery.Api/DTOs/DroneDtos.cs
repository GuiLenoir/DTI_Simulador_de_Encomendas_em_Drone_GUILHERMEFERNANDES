using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.DTOs;

public sealed record CreateDroneRequest(
    string Code,
    string Name,
    decimal MaxPackageWeightKg,
    decimal MaxRangeKm,
    decimal BatteryLevelPercent,
    decimal AverageSpeedKmPerHour,
    decimal BatteryConsumptionPercentagePerKm,
    decimal CurrentX,
    decimal CurrentY,
    DroneStatus Status,
    string? Notes,
    bool IsActive);

public sealed record UpdateDroneRequest(
    string Code,
    string Name,
    decimal MaxPackageWeightKg,
    decimal MaxRangeKm,
    decimal BatteryLevelPercent,
    decimal AverageSpeedKmPerHour,
    decimal BatteryConsumptionPercentagePerKm,
    decimal CurrentX,
    decimal CurrentY,
    DroneStatus Status,
    string? Notes,
    bool IsActive);

public sealed record DroneResponse(
    int Id,
    string Code,
    string Name,
    decimal MaxPackageWeightKg,
    decimal MaxRangeKm,
    decimal BatteryLevelPercent,
    decimal BatterySafetyMarginPercentagePoints,
    decimal AverageSpeedKmPerHour,
    decimal BatteryConsumptionPercentagePerKm,
    decimal CurrentX,
    decimal CurrentY,
    DroneStatus Status,
    string? Notes,
    bool IsActive,
    bool HasExecutingTrip,
    bool HasPlannedTrips,
    DateTime? ChargingStartedAtUtc,
    DateTime? ChargingCompletedAtUtc,
    int ChargingProgressPercentage,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record DroneSettingsResponse(
    decimal BatterySafetyMarginPercentagePoints,
    DateTime UpdatedAtUtc);

public sealed record UpdateDroneSettingsRequest(decimal BatterySafetyMarginPercentagePoints);
