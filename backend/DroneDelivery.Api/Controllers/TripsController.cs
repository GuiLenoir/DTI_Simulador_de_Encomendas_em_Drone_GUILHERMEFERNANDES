using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/trips")]
public sealed class TripsController : ControllerBase
{
    private readonly IDeliveryPlanningService _deliveryPlanningService;
    private readonly IUpcomingTripService _upcomingTripService;

    public TripsController(IDeliveryPlanningService deliveryPlanningService, IUpcomingTripService upcomingTripService)
    {
        _deliveryPlanningService = deliveryPlanningService;
        _upcomingTripService = upcomingTripService;
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<UpcomingTripsResponse>> GetUpcoming(CancellationToken cancellationToken) =>
        Ok(await _upcomingTripService.GetAsync(cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TripResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _deliveryPlanningService.GetTripsAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TripResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _deliveryPlanningService.GetTripByIdAsync(id, cancellationToken));
}
