using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/deliveries")]
public sealed class DeliveriesController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;

    public DeliveriesController(IDeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeliveryResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _deliveryService.GetAllAsync(cancellationToken));

    [HttpGet("routes")]
    public async Task<ActionResult<IReadOnlyList<DeliveryResponse>>> GetRoutes(CancellationToken cancellationToken) =>
        Ok(await _deliveryService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeliveryResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _deliveryService.GetByIdAsync(id, cancellationToken));

    [HttpPost("allocate/{orderId:int}")]
    public async Task<ActionResult<DeliveryResponse>> Allocate(int orderId, CancellationToken cancellationToken)
    {
        var response = await _deliveryService.AllocateAsync(orderId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPost("simulate/{deliveryId:int}")]
    public async Task<ActionResult<DeliveryResponse>> Simulate(int deliveryId, CancellationToken cancellationToken) =>
        Ok(await _deliveryService.SimulateAsync(deliveryId, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _deliveryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
