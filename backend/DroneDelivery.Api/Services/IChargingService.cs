using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public interface IChargingService
{
    DroneRuntimeState GetCurrentState(Drone drone, DateTime utcNow);
    void StartChargingIfNeeded(Drone drone, decimal batteryAtReturnPercentage, DateTime completedAtUtc);
}
