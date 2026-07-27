using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.DTOs;

public sealed record UpcomingTripsResponse(
    DateTime GeneratedAtUtc,
    IReadOnlyList<UpcomingTripResponse> UpcomingTrips,
    IReadOnlyList<UnplannedOrderResponse> UnplannedOrders);

public sealed record UpcomingTripResponse(
    int? TripId,
    string? DroneCode,
    IReadOnlyList<UpcomingTripOrderResponse> Orders,
    decimal TotalWeightKg,
    decimal? DroneCapacityKg,
    decimal CapacityUsagePercentage,
    decimal EstimatedDistanceKm,
    decimal EstimatedBatteryConsumptionPercentagePoints,
    decimal BatterySafetyMarginPercentagePoints,
    decimal MinimumRequiredBatteryPercentage,
    DateTime? EstimatedStartAtUtc,
    string WaitingCode,
    string WaitingReason,
    string FriendlyStatus,
    int? BlockingTripId);

public sealed record UpcomingTripOrderResponse(
    int OrderId,
    string OrderCode,
    string CustomerName,
    OrderPriority Priority,
    decimal PackageWeightKg);

public sealed record UnplannedOrderResponse(
    int OrderId,
    string OrderCode,
    string CustomerName,
    OrderPriority Priority,
    decimal PackageWeightKg,
    DateTime? QueuedAtUtc,
    string WaitingCode,
    string WaitingReason);
