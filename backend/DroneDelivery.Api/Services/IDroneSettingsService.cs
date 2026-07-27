using DroneDelivery.Api.DTOs;

namespace DroneDelivery.Api.Services;

public interface IDroneSettingsService
{
    Task<DroneSettingsResponse> GetAsync(CancellationToken cancellationToken);
    Task<DroneSettingsResponse> UpdateAsync(UpdateDroneSettingsRequest request, CancellationToken cancellationToken);
}
