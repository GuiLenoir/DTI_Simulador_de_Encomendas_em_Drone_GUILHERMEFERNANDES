using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public interface IDroneOrderCapabilityService
{
    Task<bool> CanServeAnyPendingOrderAsync(Drone drone, decimal availableBatteryPercentage, CancellationToken cancellationToken);
}
