namespace DroneDelivery.Api.Models;

public enum OrderQueueStatus
{
    NotQueued = 1,
    Queued = 2,
    Planned = 3,
    Allocated = 4,
    Completed = 5,
    Cancelled = 6
}
