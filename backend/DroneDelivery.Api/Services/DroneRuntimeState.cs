using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public sealed record DroneRuntimeState(
    DroneStatus Status,
    decimal BatteryLevelPercent,
    int ChargingProgressPercentage,
    DateTime? ChargingStartedAtUtc,
    DateTime? ChargingCompletedAtUtc);
