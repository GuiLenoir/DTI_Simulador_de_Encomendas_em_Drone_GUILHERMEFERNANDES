using DroneDelivery.Api.DTOs;

namespace DroneDelivery.Api.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderResponse>> GetQueueAsync(CancellationToken cancellationToken);
    Task<OrderResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderResponse> UpdateAsync(int id, UpdateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderResponse> QueueAsync(int id, CancellationToken cancellationToken);
    Task<OrderResponse> RemoveFromQueueAsync(int id, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
