using DroneDelivery.Api.DTOs;

namespace DroneDelivery.Api.Services;

public interface ICustomerSimulationService
{
    Task<CustomerOrderCreatedResponse> CreateOrderAsync(CustomerOrderRequest request, CancellationToken cancellationToken);
    Task<CustomerTrackingResponse> GetTrackingAsync(int orderId, CancellationToken cancellationToken);
}
