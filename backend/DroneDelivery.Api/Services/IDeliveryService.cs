using DroneDelivery.Api.DTOs;

namespace DroneDelivery.Api.Services;

public interface IDeliveryService
{
    Task<IReadOnlyList<DeliveryResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<DeliveryResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<DeliveryResponse> AllocateAsync(int orderId, CancellationToken cancellationToken);
    Task<DeliveryResponse> SimulateAsync(int deliveryId, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
