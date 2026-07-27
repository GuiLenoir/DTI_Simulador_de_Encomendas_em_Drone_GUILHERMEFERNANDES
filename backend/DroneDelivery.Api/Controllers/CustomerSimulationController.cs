using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/customer-simulation")]
public sealed class CustomerSimulationController : ControllerBase
{
    private readonly ICustomerSimulationService _simulationService;

    public CustomerSimulationController(ICustomerSimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    [HttpPost("orders")]
    public async Task<ActionResult<CustomerOrderCreatedResponse>> CreateOrder(CustomerOrderRequest request, CancellationToken cancellationToken)
    {
        var response = await _simulationService.CreateOrderAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTracking), new { id = response.OrderId }, response);
    }

    [HttpGet("orders/{id:int}/tracking")]
    public async Task<ActionResult<CustomerTrackingResponse>> GetTracking(int id, CancellationToken cancellationToken) =>
        Ok(await _simulationService.GetTrackingAsync(id, cancellationToken));
}
