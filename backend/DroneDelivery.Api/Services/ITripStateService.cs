using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public interface ITripStateService
{
    TripStateSnapshot GetCurrentState(Trip trip, DateTime utcNow);
    bool IsMutable(Trip trip, DateTime utcNow);
}
