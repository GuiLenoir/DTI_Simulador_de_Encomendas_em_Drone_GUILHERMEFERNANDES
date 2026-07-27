using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/drone-settings")]
public sealed class DroneSettingsController : ControllerBase
{
    private readonly IDroneSettingsService _settingsService;

    public DroneSettingsController(IDroneSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<DroneSettingsResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await _settingsService.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<DroneSettingsResponse>> Update(UpdateDroneSettingsRequest request, CancellationToken cancellationToken) =>
        Ok(await _settingsService.UpdateAsync(request, cancellationToken));
}
