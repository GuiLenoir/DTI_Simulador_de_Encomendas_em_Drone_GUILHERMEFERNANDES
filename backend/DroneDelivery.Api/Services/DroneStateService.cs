using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public sealed class DroneStateService : IDroneStateService
{
    private static readonly IReadOnlyDictionary<DroneStatus, DroneStatus[]> AllowedTransitions =
        new Dictionary<DroneStatus, DroneStatus[]>
        {
            [DroneStatus.Idle] = [DroneStatus.Loading, DroneStatus.Charging],
            [DroneStatus.Loading] = [DroneStatus.Flying],
            [DroneStatus.Flying] = [DroneStatus.Delivering],
            [DroneStatus.Delivering] = [DroneStatus.Returning],
            [DroneStatus.Returning] = [DroneStatus.Idle, DroneStatus.Charging],
            [DroneStatus.Charging] = [DroneStatus.Idle]
        };

    public void Transition(Drone drone, DroneStatus nextStatus)
    {
        if (!AllowedTransitions[drone.Status].Contains(nextStatus))
        {
            throw new ValidationException(
                "INVALID_DRONE_STATE_TRANSITION",
                "Invalid drone state transition",
                $"Cannot transition drone from {drone.Status} to {nextStatus}.");
        }

        drone.Status = nextStatus;
    }
}
