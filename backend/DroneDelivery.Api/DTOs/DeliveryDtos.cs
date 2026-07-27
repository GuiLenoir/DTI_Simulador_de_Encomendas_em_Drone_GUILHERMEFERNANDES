using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.DTOs;

public sealed record DeliveryResponse(
    int Id,
    int DroneId,
    string DroneCode,
    int OrderId,
    DeliveryStatus Status,
    decimal StartX,
    decimal StartY,
    decimal DestinationX,
    decimal DestinationY,
    decimal EndX,
    decimal EndY,
    decimal EstimatedDistanceKm,
    decimal EstimatedBatteryConsumptionPercent,
    decimal EstimatedDurationMinutes,
    DateTime AllocatedAt,
    DateTime? DeliveredAt);

public sealed record DashboardResponse(
    DateTime CurrentUtc,
    int CompletedDeliveries,
    int PendingDeliveries,
    decimal AverageDeliveryMinutes,
    string? MostEfficientDrone,
    IReadOnlyList<DashboardDroneResponse> Drones,
    IReadOnlyList<OrderResponse> Orders,
    IReadOnlyList<DashboardDeliveryResponse> ActiveDeliveries,
    IReadOnlyList<TripResponse> PlannedTrips,
    IReadOnlyList<TripResponse> ActiveTrips,
    IReadOnlyList<OrderResponse> QueuedOrders);

public sealed record DashboardDroneResponse(
    int Id,
    string Code,
    decimal BatteryLevelPercent,
    decimal CurrentX,
    decimal CurrentY,
    decimal MaxPackageWeightKg,
    decimal MaxRangeKm,
    DroneStatus Status,
    int? ActiveOrderId,
    int? ActiveDeliveryId,
    int? ActiveTripId,
    decimal BatterySafetyMarginPercentagePoints,
    DateTime? ChargingStartedAtUtc,
    DateTime? ChargingCompletedAtUtc,
    int ChargingProgressPercentage);

public sealed record DashboardDeliveryResponse(
    int Id,
    int OrderId,
    int DroneId,
    string DroneCode,
    DeliveryStatus Status,
    string CurrentPhase,
    DateTime CurrentPhaseStartedAtUtc,
    DateTime NextPhaseAtUtc,
    DateTime CompletedAtUtc,
    int ElapsedSeconds,
    int RemainingPhaseSeconds,
    int ProgressPercentage,
    decimal EstimatedDistanceKm,
    decimal EstimatedBatteryConsumptionPercent,
    decimal DestinationX,
    decimal DestinationY);

public sealed record TripOrderResponse(
    int OrderId,
    string CustomerName,
    OrderPriority Priority,
    decimal PackageWeightKg,
    decimal DestinationX,
    decimal DestinationY,
    int DeliverySequence,
    DateTime EstimatedArrivalAtUtc);

public sealed record TripResponse(
    int Id,
    int DroneId,
    string DroneCode,
    TripStatus Status,
    string CurrentPhase,
    DateTime PlannedAtUtc,
    DateTime LoadingStartedAtUtc,
    DateTime FlyingStartedAtUtc,
    DateTime DeliveringStartedAtUtc,
    DateTime ReturningStartedAtUtc,
    DateTime CompletedAtUtc,
    DateTime NextPhaseAtUtc,
    int RemainingPhaseSeconds,
    int ProgressPercentage,
    decimal TotalWeightKg,
    decimal MaximumWeightKg,
    decimal CapacityUsagePercentage,
    decimal EstimatedDistanceKm,
    decimal EstimatedBatteryConsumptionPercentagePoints,
    decimal BatterySafetyMarginPercentagePoints,
    decimal MinimumRequiredBatteryPercentage,
    decimal BatteryAtDeparturePercentage,
    decimal ExpectedBatteryAtReturnPercentage,
    IReadOnlyList<TripOrderResponse> Orders);

public sealed record DeliveryPlanningResponse(
    int TripsCreated,
    int OrdersAllocated,
    int OrdersRemainingQueued,
    IReadOnlyList<TripResponse> Trips,
    IReadOnlyList<UnallocatedOrderResponse> UnallocatedOrders);

public sealed record UnallocatedOrderResponse(
    int OrderId,
    string CustomerName,
    string Reason);
