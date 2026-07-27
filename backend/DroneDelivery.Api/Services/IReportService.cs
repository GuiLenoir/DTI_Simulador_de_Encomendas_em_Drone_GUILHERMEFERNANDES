using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public interface IReportService
{
    Task<ReportResponse> GetAsync(DateTime? from, DateTime? to, int? droneId, OrderPriority? priority, CancellationToken cancellationToken);
}
