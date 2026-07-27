using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public interface IDeliveryStateService
{
    DeliveryStateSnapshot GetCurrentState(Delivery delivery, DateTime utcNow);
    bool IsActive(Delivery delivery, DateTime utcNow);
}
