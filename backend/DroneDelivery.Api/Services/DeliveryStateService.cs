using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public sealed class DeliveryStateService : IDeliveryStateService
{
    public DeliveryStateSnapshot GetCurrentState(Delivery delivery, DateTime utcNow)
    {
        if (utcNow < delivery.FlyingStartedAtUtc)
        {
            return CreateSnapshot(delivery, utcNow, DroneStatus.Loading, DeliveryStatus.Allocated, OrderStatus.InTransit,
                "Loading", delivery.LoadingStartedAtUtc, delivery.FlyingStartedAtUtc, isActive: true);
        }

        if (utcNow < delivery.DeliveringStartedAtUtc)
        {
            return CreateSnapshot(delivery, utcNow, DroneStatus.Flying, DeliveryStatus.InTransit, OrderStatus.InTransit,
                "Flying", delivery.FlyingStartedAtUtc, delivery.DeliveringStartedAtUtc, isActive: true);
        }

        if (utcNow < delivery.ReturningStartedAtUtc)
        {
            return CreateSnapshot(delivery, utcNow, DroneStatus.Delivering, DeliveryStatus.InTransit, OrderStatus.InTransit,
                "Delivering", delivery.DeliveringStartedAtUtc, delivery.ReturningStartedAtUtc, isActive: true);
        }

        if (utcNow < delivery.CompletedAtUtc)
        {
            return CreateSnapshot(delivery, utcNow, DroneStatus.Returning, DeliveryStatus.InTransit, OrderStatus.Delivered,
                "Returning", delivery.ReturningStartedAtUtc, delivery.CompletedAtUtc, isActive: true);
        }

        return CreateSnapshot(delivery, utcNow, DroneStatus.Idle, DeliveryStatus.Delivered, OrderStatus.Delivered,
            "Completed", delivery.CompletedAtUtc, delivery.CompletedAtUtc, isActive: false);
    }

    public bool IsActive(Delivery delivery, DateTime utcNow) => utcNow < delivery.CompletedAtUtc;

    private static DeliveryStateSnapshot CreateSnapshot(
        Delivery delivery,
        DateTime utcNow,
        DroneStatus droneStatus,
        DeliveryStatus deliveryStatus,
        OrderStatus orderStatus,
        string currentPhase,
        DateTime currentPhaseStartedAtUtc,
        DateTime nextPhaseAtUtc,
        bool isActive)
    {
        var totalSeconds = Math.Max(1, (int)Math.Ceiling((delivery.CompletedAtUtc - delivery.CreatedAtUtc).TotalSeconds));
        var elapsedSeconds = Math.Clamp((int)Math.Floor((utcNow - delivery.CreatedAtUtc).TotalSeconds), 0, totalSeconds);
        var remainingPhaseSeconds = Math.Max(0, (int)Math.Ceiling((nextPhaseAtUtc - utcNow).TotalSeconds));
        var progressPercentage = Math.Clamp((int)Math.Floor(elapsedSeconds / (double)totalSeconds * 100), 0, 100);

        return new DeliveryStateSnapshot(
            droneStatus,
            deliveryStatus,
            orderStatus,
            currentPhase,
            currentPhaseStartedAtUtc,
            nextPhaseAtUtc,
            delivery.CompletedAtUtc,
            elapsedSeconds,
            remainingPhaseSeconds,
            progressPercentage,
            isActive);
    }
}
