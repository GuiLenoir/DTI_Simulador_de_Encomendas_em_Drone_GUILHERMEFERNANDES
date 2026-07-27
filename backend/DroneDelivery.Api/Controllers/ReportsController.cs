using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<ActionResult<ReportResponse>> Get(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? droneId,
        [FromQuery] OrderPriority? priority,
        CancellationToken cancellationToken) =>
        Ok(await _reportService.GetAsync(from, to, droneId, priority, cancellationToken));
}
