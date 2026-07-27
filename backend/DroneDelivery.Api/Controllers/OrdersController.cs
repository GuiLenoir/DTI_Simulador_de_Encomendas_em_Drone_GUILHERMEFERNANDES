using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _orderService.GetAllAsync(cancellationToken));

    [HttpGet("queue")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetQueue(CancellationToken cancellationToken) =>
        Ok(await _orderService.GetQueueAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _orderService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var response = await _orderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderResponse>> Update(int id, UpdateOrderRequest request, CancellationToken cancellationToken) =>
        Ok(await _orderService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/queue")]
    public async Task<ActionResult<OrderResponse>> Queue(int id, CancellationToken cancellationToken) =>
        Ok(await _orderService.QueueAsync(id, cancellationToken));

    [HttpDelete("{id:int}/queue")]
    public async Task<ActionResult<OrderResponse>> RemoveFromQueue(int id, CancellationToken cancellationToken) =>
        Ok(await _orderService.RemoveFromQueueAsync(id, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _orderService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
