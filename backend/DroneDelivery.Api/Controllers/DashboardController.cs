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

    public DashboardController(IDashboardService dashboardService, IDeliveryPlanningService deliveryPlanningService)
    {
        _dashboardService = dashboardService;
        _deliveryPlanningService = deliveryPlanningService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
    {
        await _deliveryPlanningService.ProcessQueueAsync(cancellationToken);
        return Ok(await _dashboardService.GetAsync(cancellationToken));
    }
}
