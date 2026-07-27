using DroneDelivery.Api.Data;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Api.Services;

public sealed class DroneOrderCapabilityService : IDroneOrderCapabilityService
{
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IRoutePlanningService _routePlanningService;
    private readonly IDroneSettingsService _settingsService;
    private readonly DroneDeliveryOptions _options;

    public DroneOrderCapabilityService(
        DroneDeliveryDbContext dbContext,
        IRoutePlanningService routePlanningService,
        IDroneSettingsService settingsService,
        IOptions<DroneDeliveryOptions> options)
    {
        _dbContext = dbContext;
        _routePlanningService = routePlanningService;
        _settingsService = settingsService;
        _options = options.Value;
    }

    public async Task<bool> CanServeAnyPendingOrderAsync(Drone drone, decimal availableBatteryPercentage, CancellationToken cancellationToken)
    {
        if (!drone.IsActive)
        {
            return false;
        }

        var safetyMargin = (await _settingsService.GetAsync(cancellationToken)).BatterySafetyMarginPercentagePoints;
        var orders = await _dbContext.Orders
            .Where(order => order.Status == OrderStatus.Pending)
            .Where(order => order.QueueStatus == OrderQueueStatus.Queued || order.QueueStatus == OrderQueueStatus.NotQueued)
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.QueuedAtUtc ?? order.CreatedAt)
            .ThenBy(order => order.Id)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            if (order.PackageWeightKg > drone.MaxPackageWeightKg)
            {
                continue;
            }

            var distance = await TryCalculateRouteDistanceAsync(order, cancellationToken);
            if (distance is null || distance > drone.MaxRangeKm)
            {
                continue;
            }

            var requiredBattery = Math.Round(distance.Value * drone.BatteryConsumptionPercentagePerKm, 2) + safetyMargin;
            if (requiredBattery <= 100m && availableBatteryPercentage >= requiredBattery)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<decimal?> TryCalculateRouteDistanceAsync(DeliveryOrder order, CancellationToken cancellationToken)
    {
        var start = new RoutePoint(_options.BaseX, _options.BaseY);
        var end = _options.RequireReturnToBase
            ? new RoutePoint(_options.BaseX, _options.BaseY)
            : new RoutePoint(order.DestinationX, order.DestinationY);

        try
        {
            return await _routePlanningService.CalculateDistanceAsync(
                start,
                new[] { new RoutePoint(order.DestinationX, order.DestinationY) },
                end,
                cancellationToken);
        }
        catch (ValidationException exception) when (exception.Code is "ROUTE_BLOCKED_BY_NO_FLY_ZONE" or "NO_VALID_ROUTE_AVAILABLE")
        {
            return null;
        }
    }
}
