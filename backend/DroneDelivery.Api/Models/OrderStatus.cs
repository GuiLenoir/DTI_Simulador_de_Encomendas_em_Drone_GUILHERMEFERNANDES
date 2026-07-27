namespace DroneDelivery.Api.Models;

public enum OrderStatus
{
    Pending = 1,
    Allocated = 2,
    InTransit = 3,
    Delivered = 4,
    Rejected = 5
}
