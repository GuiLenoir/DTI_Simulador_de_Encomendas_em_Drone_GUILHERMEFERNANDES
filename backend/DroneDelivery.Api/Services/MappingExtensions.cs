using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public static class MappingExtensions
{
    public static DroneResponse ToResponse(this Drone drone) =>
        new(drone.Id, drone.Code, drone.Name, drone.MaxPackageWeightKg, drone.MaxRangeKm, drone.BatteryLevelPercent,
            drone.BatterySafetyMarginPercentagePoints, drone.AverageSpeedKmPerHour, drone.BatteryConsumptionPercentagePerKm,
            drone.CurrentX, drone.CurrentY, drone.Status, drone.Notes, drone.IsActive, false, false,
            drone.ChargingStartedAtUtc, drone.ChargingCompletedAtUtc, 0, drone.CreatedAt, drone.UpdatedAt);

    public static OrderResponse ToResponse(this DeliveryOrder order) =>
        new(order.Id, order.CustomerName, order.DestinationX, order.DestinationY, order.PackageWeightKg,
            order.Priority, order.Status, order.QueueStatus, order.QueuedAtUtc, order.CreatedAt, order.UpdatedAt);

    public static DeliveryResponse ToResponse(this Delivery delivery) =>
        new(delivery.Id, delivery.DroneId, delivery.Drone.Code, delivery.OrderId, delivery.Status,
            delivery.StartX, delivery.StartY, delivery.DestinationX, delivery.DestinationY,
            delivery.EndX, delivery.EndY, delivery.EstimatedDistanceKm,
            delivery.EstimatedBatteryConsumptionPercent, delivery.EstimatedDurationMinutes,
            delivery.AllocatedAt, delivery.DeliveredAt);
}
