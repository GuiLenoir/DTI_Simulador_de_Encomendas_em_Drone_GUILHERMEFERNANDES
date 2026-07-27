namespace DroneDelivery.Api.Models;

public enum DroneStatus
{
    Idle = 1,
    Loading = 2,
    Flying = 3,
    Delivering = 4,
    Returning = 5,
    Charging = 6,
    Maintenance = 7,
    Unavailable = 8
}
