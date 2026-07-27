using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IDeliveryPlanningService _deliveryPlanningService;
    private readonly IDeliveryService _deliveryService;

    public DashboardController(IDashboardService dashboardService, IDeliveryPlanningService deliveryPlanningService, IDeliveryService deliveryService)
    {
        _dashboardService = dashboardService;
        _deliveryPlanningService = deliveryPlanningService;
        _deliveryService = deliveryService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
    {
        await _deliveryService.CompleteElapsedAsync(cancellationToken);
        await _deliveryPlanningService.ProcessQueueAsync(cancellationToken);
        return Ok(await _dashboardService.GetAsync(cancellationToken));
    }
}
