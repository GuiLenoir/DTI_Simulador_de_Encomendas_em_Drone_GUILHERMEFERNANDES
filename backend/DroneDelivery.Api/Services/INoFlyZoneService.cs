using DroneDelivery.Api.DTOs;

namespace DroneDelivery.Api.Services;

public interface INoFlyZoneService
{
    Task<IReadOnlyList<NoFlyZoneResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<NoFlyZoneResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<NoFlyZoneResponse> CreateAsync(CreateNoFlyZoneRequest request, CancellationToken cancellationToken);
    Task<NoFlyZoneResponse> UpdateAsync(int id, UpdateNoFlyZoneRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
