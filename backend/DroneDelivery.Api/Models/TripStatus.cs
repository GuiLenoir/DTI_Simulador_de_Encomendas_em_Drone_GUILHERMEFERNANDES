namespace DroneDelivery.Api.Models;

public enum TripStatus
{
    Planned = 1,
    Loading = 2,
    Flying = 3,
    Delivering = 4,
    Returning = 5,
    Completed = 6,
    Cancelled = 7
}
