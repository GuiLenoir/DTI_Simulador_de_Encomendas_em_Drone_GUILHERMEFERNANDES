using DroneDelivery.Api.DTOs;

namespace DroneDelivery.Api.Services;

public interface IUpcomingTripService
{
    Task<UpcomingTripsResponse> GetAsync(CancellationToken cancellationToken);
}
