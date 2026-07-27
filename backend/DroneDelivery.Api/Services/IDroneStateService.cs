using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public interface IDroneStateService
{
    void Transition(Drone drone, DroneStatus nextStatus);
}
