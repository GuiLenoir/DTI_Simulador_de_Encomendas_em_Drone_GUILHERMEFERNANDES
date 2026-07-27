using DroneDelivery.Api.DTOs;

namespace DroneDelivery.Api.Services;

public interface IDeliveryPlanningService
{
    Task<DeliveryPlanningResponse> PlanAsync(CancellationToken cancellationToken);
    Task<DeliveryPlanningResponse> ProcessQueueAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TripResponse>> GetTripsAsync(CancellationToken cancellationToken);
    Task<TripResponse> GetTripByIdAsync(int id, CancellationToken cancellationToken);
}
