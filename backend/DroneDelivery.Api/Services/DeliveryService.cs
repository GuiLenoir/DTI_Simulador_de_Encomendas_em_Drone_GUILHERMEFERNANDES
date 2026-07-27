using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Api.Services;

public sealed class DeliveryService : IDeliveryService
{
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IDistanceService _distanceService;
    private readonly IRoutePlanningService _routePlanningService;
    private readonly IDroneStateService _droneStateService;
    private readonly IDeliveryStateService _deliveryStateService;
    private readonly ITripStateService _tripStateService;
    private readonly IChargingService _chargingService;
    private readonly IDroneOrderCapabilityService _droneOrderCapabilityService;
    private readonly IClock _clock;
    private readonly IDroneSettingsService _droneSettingsService;
    private readonly DroneDeliveryOptions _options;
    private readonly SimulationOptions _simulationOptions;

    public DeliveryService(
        DroneDeliveryDbContext dbContext,
        IDistanceService distanceService,
        IRoutePlanningService routePlanningService,
        IDroneStateService droneStateService,
        IDeliveryStateService deliveryStateService,
        ITripStateService tripStateService,
        IChargingService chargingService,
        IDroneOrderCapabilityService droneOrderCapabilityService,
        IClock clock,
        IDroneSettingsService droneSettingsService,
        IOptions<DroneDeliveryOptions> options,
        IOptions<SimulationOptions> simulationOptions)
    {
        _dbContext = dbContext;
        _distanceService = distanceService;
        _routePlanningService = routePlanningService;
        _droneStateService = droneStateService;
        _deliveryStateService = deliveryStateService;
        _tripStateService = tripStateService;
        _chargingService = chargingService;
        _droneOrderCapabilityService = droneOrderCapabilityService;
        _clock = clock;
        _droneSettingsService = droneSettingsService;
        _options = options.Value;
        _simulationOptions = simulationOptions.Value;
    }

    public async Task<IReadOnlyList<DeliveryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        await CompleteElapsedDeliveriesAsync(utcNow, cancellationToken);

        var deliveries = await _dbContext.Deliveries
            .Include(delivery => delivery.Drone)
            .OrderByDescending(delivery => delivery.AllocatedAt)
            .ToListAsync(cancellationToken);

        return deliveries
            .Select(delivery => MapResponse(delivery, utcNow))
            .ToList();
    }

    public async Task<DeliveryResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        await CompleteElapsedDeliveriesAsync(utcNow, cancellationToken);
        var safetyMargin = (await _droneSettingsService.GetAsync(cancellationToken)).BatterySafetyMarginPercentagePoints;

        var delivery = await FindAsync(id, cancellationToken);
        return MapResponse(delivery, utcNow);
    }

    public async Task<DeliveryResponse> AllocateAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new NotFoundException($"Order {orderId} was not found.");

        if (order.Status != OrderStatus.Pending)
        {
            throw new ValidationException("ORDER_NOT_PENDING", "Order is not pending", "Only pending orders can be allocated.");
        }

        var utcNow = _clock.UtcNow;
        await CompleteElapsedDeliveriesAsync(utcNow, cancellationToken);
        var safetyMargin = (await _droneSettingsService.GetAsync(cancellationToken)).BatterySafetyMarginPercentagePoints;

        var activeDroneIds = await _dbContext.Deliveries
            .Where(delivery => delivery.CompletedAtUtc > utcNow)
            .Select(delivery => delivery.DroneId)
            .ToListAsync(cancellationToken);
        var activeTripDroneIds = await _dbContext.Trips
            .Where(trip => trip.CompletedAtUtc > utcNow)
            .Select(trip => trip.DroneId)
            .ToListAsync(cancellationToken);

        var drones = await _dbContext.Drones
            .Where(drone => drone.IsActive)
            .Where(drone => !activeDroneIds.Contains(drone.Id))
            .Where(drone => !activeTripDroneIds.Contains(drone.Id))
            .ToListAsync(cancellationToken);

        var candidates = new List<AllocationCandidate>();
        foreach (var drone in drones.Where(drone => _chargingService.GetCurrentState(drone, utcNow).Status == DroneStatus.Idle))
        {
            if (drone.MaxPackageWeightKg < order.PackageWeightKg)
            {
                continue;
            }

            var routeDistance = await CalculateRouteDistanceAsync(drone, order, cancellationToken);
            if (drone.MaxRangeKm < routeDistance)
            {
                continue;
            }

            var minimumBattery = CalculateMinimumRequiredBattery(drone, routeDistance, safetyMargin);
            if (drone.BatteryLevelPercent < minimumBattery)
            {
                continue;
            }

            candidates.Add(new AllocationCandidate(drone, routeDistance));
        }

        var selected = candidates
            .OrderBy(candidate => candidate.DistanceKm)
            .ThenByDescending(candidate => candidate.Drone.BatteryLevelPercent)
            .ThenBy(candidate => candidate.Drone.Code)
            .FirstOrDefault();

        if (selected is null)
        {
            throw new AllocationException("No available drone can carry this package, complete the route, and satisfy battery requirements.");
        }

        var batteryConsumption = CalculateBatteryConsumption(selected.Drone, selected.DistanceKm);
        var durationMinutes = CalculateDurationMinutes(selected.Drone, selected.DistanceKm);
        var outboundDistance = _distanceService.Calculate(selected.Drone.CurrentX, selected.Drone.CurrentY, order.DestinationX, order.DestinationY);
        var returnDistance = _options.RequireReturnToBase
            ? _distanceService.Calculate(order.DestinationX, order.DestinationY, _options.BaseX, _options.BaseY)
            : 0m;
        var outboundFlightDurationSeconds = CalculateFlightDurationSeconds(outboundDistance);
        var returnFlightDurationSeconds = CalculateFlightDurationSeconds(returnDistance);
        var loadingStartedAtUtc = utcNow;
        var flyingStartedAtUtc = loadingStartedAtUtc.AddSeconds(_simulationOptions.LoadingDurationSeconds);
        var deliveringStartedAtUtc = flyingStartedAtUtc.AddSeconds(outboundFlightDurationSeconds);
        var returningStartedAtUtc = deliveringStartedAtUtc.AddSeconds(_simulationOptions.DeliveryDurationSeconds);
        var completedAtUtc = returningStartedAtUtc.AddSeconds(returnFlightDurationSeconds);

        selected.Drone.Status = DroneStatus.Idle;
        _droneStateService.Transition(selected.Drone, DroneStatus.Loading);
        selected.Drone.ChargingStartedAtUtc = null;
        selected.Drone.BatteryAtChargingStartPercentage = null;
        selected.Drone.ChargingCompletedAtUtc = null;
        order.Status = OrderStatus.InTransit;
        order.QueueStatus = OrderQueueStatus.Allocated;

        var delivery = new Delivery
        {
            DroneId = selected.Drone.Id,
            OrderId = order.Id,
            Status = DeliveryStatus.Allocated,
            StartX = selected.Drone.CurrentX,
            StartY = selected.Drone.CurrentY,
            DestinationX = order.DestinationX,
            DestinationY = order.DestinationY,
            EndX = _options.BaseX,
            EndY = _options.BaseY,
            EstimatedDistanceKm = selected.DistanceKm,
            EstimatedBatteryConsumptionPercent = batteryConsumption,
            EstimatedDurationMinutes = durationMinutes,
            AllocatedAt = utcNow,
            CreatedAtUtc = utcNow,
            LoadingStartedAtUtc = loadingStartedAtUtc,
            FlyingStartedAtUtc = flyingStartedAtUtc,
            DeliveringStartedAtUtc = deliveringStartedAtUtc,
            ReturningStartedAtUtc = returningStartedAtUtc,
            CompletedAtUtc = completedAtUtc,
            LoadingDurationSeconds = _simulationOptions.LoadingDurationSeconds,
            OutboundFlightDurationSeconds = outboundFlightDurationSeconds,
            DeliveryDurationSeconds = _simulationOptions.DeliveryDurationSeconds,
            ReturnFlightDurationSeconds = returnFlightDurationSeconds
        };

        _dbContext.Deliveries.Add(delivery);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(delivery.Id, cancellationToken);
    }

    public async Task<DeliveryResponse> SimulateAsync(int deliveryId, CancellationToken cancellationToken)
    {
        var delivery = await FindAsync(deliveryId, cancellationToken);

        if (!_deliveryStateService.IsActive(delivery, _clock.UtcNow))
        {
            await ApplyCompletedStateAsync(delivery, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return delivery.ToResponse();
        }
        var state = _deliveryStateService.GetCurrentState(delivery, _clock.UtcNow);
        delivery.Drone.Status = state.DroneStatus;
        delivery.Order.Status = state.OrderStatus;
        delivery.Status = state.DeliveryStatus;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return delivery.ToResponse();
    }

    public async Task CompleteElapsedAsync(CancellationToken cancellationToken)
    {
        await CompleteElapsedDeliveriesAsync(_clock.UtcNow, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var delivery = await FindAsync(id, cancellationToken);
        _dbContext.Deliveries.Remove(delivery);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Delivery> FindAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.Deliveries
            .Include(delivery => delivery.Drone)
            .Include(delivery => delivery.Order)
            .FirstOrDefaultAsync(delivery => delivery.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Delivery {id} was not found.");
    }

    private async Task<decimal> CalculateRouteDistanceAsync(Drone drone, DeliveryOrder order, CancellationToken cancellationToken)
    {
        var start = new RoutePoint(drone.CurrentX, drone.CurrentY);
        var end = _options.RequireReturnToBase
            ? new RoutePoint(_options.BaseX, _options.BaseY)
            : new RoutePoint(order.DestinationX, order.DestinationY);
        return await _routePlanningService.CalculateDistanceAsync(
            start,
            new[] { new RoutePoint(order.DestinationX, order.DestinationY) },
            end,
            cancellationToken);
    }

    private decimal CalculateBatteryConsumption(Drone drone, decimal distanceKm) =>
        Math.Round(distanceKm * drone.BatteryConsumptionPercentagePerKm, 2);

    private decimal CalculateMinimumRequiredBattery(Drone drone, decimal distanceKm, decimal safetyMargin) =>
        CalculateBatteryConsumption(drone, distanceKm) + safetyMargin;

    private static decimal CalculateDurationMinutes(Drone drone, decimal distanceKm) =>
        drone.AverageSpeedKmPerHour <= 0 ? 0 : Math.Round(distanceKm / drone.AverageSpeedKmPerHour * 60, 2);

    private int CalculateFlightDurationSeconds(decimal distanceKm)
    {
        var seconds = (int)Math.Ceiling(distanceKm * _simulationOptions.SecondsPerKilometer);
        return Math.Max(1, seconds);
    }

    private async Task CompleteElapsedDeliveriesAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var elapsedDeliveries = await _dbContext.Deliveries
            .Include(delivery => delivery.Drone)
            .Include(delivery => delivery.Order)
            .Where(delivery => delivery.CompletedAtUtc <= utcNow && delivery.Status != DeliveryStatus.Delivered)
            .ToListAsync(cancellationToken);

        foreach (var delivery in elapsedDeliveries)
        {
            await ApplyCompletedStateAsync(delivery, cancellationToken);
        }

        if (elapsedDeliveries.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ApplyCompletedStateAsync(Delivery delivery, CancellationToken cancellationToken)
    {
        delivery.Drone.CurrentX = _options.BaseX;
        delivery.Drone.CurrentY = _options.BaseY;
        var batteryAtReturn = Math.Max(0, delivery.Drone.BatteryLevelPercent - delivery.EstimatedBatteryConsumptionPercent);
        if (batteryAtReturn >= 100m || await _droneOrderCapabilityService.CanServeAnyPendingOrderAsync(delivery.Drone, batteryAtReturn, cancellationToken))
        {
            delivery.Drone.Status = DroneStatus.Idle;
            delivery.Drone.BatteryLevelPercent = batteryAtReturn;
            delivery.Drone.ChargingStartedAtUtc = null;
            delivery.Drone.BatteryAtChargingStartPercentage = null;
            delivery.Drone.ChargingCompletedAtUtc = null;
        }
        else
        {
            _chargingService.StartChargingIfNeeded(delivery.Drone, batteryAtReturn, delivery.CompletedAtUtc);
        }
        delivery.Order.Status = OrderStatus.Delivered;
        delivery.Order.QueueStatus = OrderQueueStatus.Completed;
        delivery.Status = DeliveryStatus.Delivered;
        delivery.DeliveredAt ??= delivery.CompletedAtUtc;
    }

    private DeliveryResponse MapResponse(Delivery delivery, DateTime utcNow)
    {
        var state = _deliveryStateService.GetCurrentState(delivery, utcNow);
        return new DeliveryResponse(
            delivery.Id,
            delivery.DroneId,
            delivery.Drone.Code,
            delivery.OrderId,
            state.DeliveryStatus,
            delivery.StartX,
            delivery.StartY,
            delivery.DestinationX,
            delivery.DestinationY,
            delivery.EndX,
            delivery.EndY,
            delivery.EstimatedDistanceKm,
            delivery.EstimatedBatteryConsumptionPercent,
            delivery.EstimatedDurationMinutes,
            delivery.AllocatedAt,
            state.DeliveryStatus == DeliveryStatus.Delivered ? delivery.DeliveredAt ?? delivery.CompletedAtUtc : delivery.DeliveredAt);
    }

    private sealed record AllocationCandidate(Drone Drone, decimal DistanceKm);
}
