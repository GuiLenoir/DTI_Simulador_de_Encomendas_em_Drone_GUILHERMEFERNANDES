using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.DTOs;

public sealed record ReportResponse(
    DeliverySummaryResponse Summary,
    DroneEfficiencyResponse? MostEfficientDrone,
    DeliveryMapResponse Map);

public sealed record DeliverySummaryResponse(
    int CompletedDeliveries,
    int AverageDeliverySeconds);

public sealed record DroneEfficiencyResponse(
    int DroneId,
    string DroneCode,
    int CompletedDeliveries,
    decimal TotalTransportedWeightKg,
    decimal TotalDistanceKm,
    decimal TotalBatteryConsumedPercentagePoints,
    decimal EfficiencyScore);

public sealed record DeliveryMapResponse(
    int DisplayedDeliveries,
    int UsedDrones,
    decimal TotalDistanceKm,
    IReadOnlyList<DeliveryMapJourneyResponse> Journeys);

public sealed record DeliveryMapJourneyResponse(
    string Id,
    int? TripId,
    int? DeliveryId,
    int DroneId,
    string DroneCode,
    DateTime CompletedAtUtc,
    decimal DistanceKm,
    IReadOnlyList<DeliveryMapPointResponse> Points);

public sealed record DeliveryMapPointResponse(
    int Sequence,
    string Type,
    int? OrderId,
    string? OrderCode,
    OrderPriority? Priority,
    decimal? WeightKg,
    decimal X,
    decimal Y,
    DateTime? CompletedAtUtc);
