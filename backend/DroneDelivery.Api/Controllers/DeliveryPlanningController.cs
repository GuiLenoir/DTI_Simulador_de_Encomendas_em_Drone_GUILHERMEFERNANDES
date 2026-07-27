using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/delivery-planning")]
public sealed class DeliveryPlanningController : ControllerBase
{
    private readonly IDeliveryPlanningService _deliveryPlanningService;

    public DeliveryPlanningController(IDeliveryPlanningService deliveryPlanningService)
    {
        _deliveryPlanningService = deliveryPlanningService;
    }

    [HttpPost("plan")]
    public async Task<ActionResult<DeliveryPlanningResponse>> Plan(CancellationToken cancellationToken) =>
        Ok(await _deliveryPlanningService.PlanAsync(cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TripResponse>>> GetTrips(CancellationToken cancellationToken) =>
        Ok(await _deliveryPlanningService.GetTripsAsync(cancellationToken));
}
