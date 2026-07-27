using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/drones")]
public sealed class DronesController : ControllerBase
{
    private readonly IDroneService _droneService;

    public DronesController(IDroneService droneService)
    {
        _droneService = droneService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DroneResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _droneService.GetAllAsync(cancellationToken));

    [HttpGet("status")]
    public async Task<ActionResult<IReadOnlyList<DroneResponse>>> GetStatus(CancellationToken cancellationToken) =>
        Ok(await _droneService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DroneResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _droneService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<DroneResponse>> Create(CreateDroneRequest request, CancellationToken cancellationToken)
    {
        var response = await _droneService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DroneResponse>> Update(int id, UpdateDroneRequest request, CancellationToken cancellationToken) =>
        Ok(await _droneService.UpdateAsync(id, request, cancellationToken));

    [HttpPatch("{id:int}/activate")]
    public async Task<ActionResult<DroneResponse>> Activate(int id, CancellationToken cancellationToken) =>
        Ok(await _droneService.ActivateAsync(id, cancellationToken));

    [HttpPatch("{id:int}/deactivate")]
    public async Task<ActionResult<DroneResponse>> Deactivate(int id, CancellationToken cancellationToken) =>
        Ok(await _droneService.DeactivateAsync(id, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _droneService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
