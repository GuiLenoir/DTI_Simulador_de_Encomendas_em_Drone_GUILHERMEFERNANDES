using DroneDelivery.Api.DTOs;

namespace DroneDelivery.Api.Services;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken);
}
