using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.DTOs;

public sealed record CustomerOrderRequest(
    string CustomerName,
    string? PackageDescription,
    decimal PackageWeightKg,
    decimal DestinationX,
    decimal DestinationY,
    OrderPriority Priority);

public sealed record CustomerOrderCreatedResponse(
    int OrderId,
    string OrderCode);

public sealed record CustomerTrackingResponse(
    int OrderId,
    string OrderCode,
    string FriendlyStatus,
    string InternalStatus,
    string? DroneCode,
    int? TripId,
    int? DeliveryId,
    OrderPriority Priority,
    decimal WeightKg,
    RoutePointResponse Destination,
    IReadOnlyList<CustomerRoutePointResponse> Route,
    DateTime? TripStartedAtUtc,
    DateTime? EstimatedCompletionAtUtc,
    int ProgressPercentage,
    decimal RemainingDistance,
    RoutePointResponse CurrentPosition,
    string FeedbackMessage);

public sealed record CustomerRoutePointResponse(
    int Sequence,
    string Type,
    int? OrderId,
    string? OrderCode,
    OrderPriority? Priority,
    decimal? WeightKg,
    decimal X,
    decimal Y);

public sealed record RoutePointResponse(decimal X, decimal Y);
