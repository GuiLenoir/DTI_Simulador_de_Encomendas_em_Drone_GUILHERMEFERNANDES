using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/no-fly-zones")]
public sealed class NoFlyZonesController : ControllerBase
{
    private readonly INoFlyZoneService _noFlyZoneService;

    public NoFlyZonesController(INoFlyZoneService noFlyZoneService)
    {
        _noFlyZoneService = noFlyZoneService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NoFlyZoneResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _noFlyZoneService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NoFlyZoneResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _noFlyZoneService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<NoFlyZoneResponse>> Create(CreateNoFlyZoneRequest request, CancellationToken cancellationToken)
    {
        var response = await _noFlyZoneService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<NoFlyZoneResponse>> Update(int id, UpdateNoFlyZoneRequest request, CancellationToken cancellationToken) =>
        Ok(await _noFlyZoneService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _noFlyZoneService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
