using DroneDelivery.Api.DTOs;

namespace DroneDelivery.Api.Services;

public interface IDroneService
{
    Task<IReadOnlyList<DroneResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<DroneResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<DroneResponse> CreateAsync(CreateDroneRequest request, CancellationToken cancellationToken);
    Task<DroneResponse> UpdateAsync(int id, UpdateDroneRequest request, CancellationToken cancellationToken);
    Task<DroneResponse> ActivateAsync(int id, CancellationToken cancellationToken);
    Task<DroneResponse> DeactivateAsync(int id, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
