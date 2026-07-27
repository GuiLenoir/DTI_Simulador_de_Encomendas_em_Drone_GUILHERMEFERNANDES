using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.DTOs;

public sealed record CreateOrderRequest(
    string CustomerName,
    decimal DestinationX,
    decimal DestinationY,
    decimal PackageWeightKg,
    OrderPriority Priority);

public sealed record UpdateOrderRequest(
    string CustomerName,
    decimal DestinationX,
    decimal DestinationY,
    decimal PackageWeightKg,
    OrderPriority Priority,
    OrderStatus Status);

public sealed record OrderResponse(
    int Id,
    string CustomerName,
    decimal DestinationX,
    decimal DestinationY,
    decimal PackageWeightKg,
    OrderPriority Priority,
    OrderStatus Status,
    OrderQueueStatus QueueStatus,
    DateTime? QueuedAtUtc,
    DateTime CreatedAt,
    DateTime UpdatedAt);
