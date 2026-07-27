using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneDelivery.Api.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IDeliveryStateService _deliveryStateService;
    private readonly ITripStateService _tripStateService;
    private readonly IChargingService _chargingService;
    private readonly IClock _clock;
    private readonly IDroneSettingsService _settingsService;

    public DashboardService(
        DroneDeliveryDbContext dbContext,
        IDeliveryStateService deliveryStateService,
        ITripStateService tripStateService,
        IChargingService chargingService,
        IClock clock,
        IDroneSettingsService settingsService)
    {
        _dbContext = dbContext;
        _deliveryStateService = deliveryStateService;
        _tripStateService = tripStateService;
        _chargingService = chargingService;
        _clock = clock;
        _settingsService = settingsService;
    }

    public async Task<DashboardResponse> GetAsync(CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var settings = await _settingsService.GetAsync(cancellationToken);
        var completedDeliveries = await _dbContext.Deliveries
            .CountAsync(delivery => delivery.CompletedAtUtc <= utcNow, cancellationToken);
        var pendingDeliveries = await _dbContext.Orders
            .CountAsync(order => order.Status == OrderStatus.Pending, cancellationToken);

        var completed = await _dbContext.Deliveries
            .Where(delivery => delivery.CompletedAtUtc <= utcNow)
            .ToListAsync(cancellationToken);
        var averageMinutes = completed.Count == 0
            ? 0
            : Math.Round(completed.Average(delivery => delivery.EstimatedDurationMinutes), 2);

        var mostEfficientDrone = await _dbContext.Deliveries
            .Where(delivery => delivery.CompletedAtUtc <= utcNow)
            .Include(delivery => delivery.Drone)
            .GroupBy(delivery => new { delivery.DroneId, delivery.Drone.Code })
            .Select(group => new { group.Key.Code, TotalDistance = group.Sum(delivery => delivery.EstimatedDistanceKm) })
            .OrderByDescending(item => item.TotalDistance)
            .ThenBy(item => item.Code)
            .Select(item => item.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var activeDeliveries = await _dbContext.Deliveries
            .Include(delivery => delivery.Drone)
            .Where(delivery => delivery.CompletedAtUtc > utcNow)
            .OrderBy(delivery => delivery.CompletedAtUtc)
            .ToListAsync(cancellationToken);
        var activeDeliveryByDroneId = activeDeliveries.ToDictionary(delivery => delivery.DroneId);
        var trips = await _dbContext.Trips
            .Include(trip => trip.Drone)
            .Include(trip => trip.TripOrders)
            .ThenInclude(tripOrder => tripOrder.Order)
            .OrderBy(trip => trip.LoadingStartedAtUtc)
            .ToListAsync(cancellationToken);
        var activeTrips = trips
            .Where(trip => _tripStateService.GetCurrentState(trip, utcNow).IsActive)
            .ToList();
        var plannedTrips = trips
            .Where(trip => _tripStateService.GetCurrentState(trip, utcNow).TripStatus == TripStatus.Planned)
            .ToList();
        var activeTripByDroneId = activeTrips.ToDictionary(trip => trip.DroneId);

        var drones = await _dbContext.Drones
            .OrderBy(drone => drone.Code)
            .ToListAsync(cancellationToken);
        var droneResponses = drones
            .Select(drone =>
            {
                activeDeliveryByDroneId.TryGetValue(drone.Id, out var activeDelivery);
                activeTripByDroneId.TryGetValue(drone.Id, out var activeTrip);
                var runtime = _chargingService.GetCurrentState(drone, utcNow);
                var status = !drone.IsActive
                    ? DroneStatus.Unavailable
                    : activeTrip is not null
                    ? _tripStateService.GetCurrentState(activeTrip, utcNow).DroneStatus
                    : activeDelivery is null
                        ? runtime.Status
                        : _deliveryStateService.GetCurrentState(activeDelivery, utcNow).DroneStatus;

                return new DashboardDroneResponse(
                    drone.Id,
                    drone.Code,
                    runtime.BatteryLevelPercent,
                    drone.CurrentX,
                    drone.CurrentY,
                    drone.MaxPackageWeightKg,
                    drone.MaxRangeKm,
                    status,
                    activeDelivery?.OrderId,
                    activeDelivery?.Id,
                    activeTrip?.Id,
                    settings.BatterySafetyMarginPercentagePoints,
                    runtime.ChargingStartedAtUtc,
                    runtime.ChargingCompletedAtUtc,
                    runtime.ChargingProgressPercentage);
            })
            .ToList();

        var orders = await _dbContext.Orders
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
        var deliveryByOrderId = await _dbContext.Deliveries
            .ToDictionaryAsync(delivery => delivery.OrderId, cancellationToken);
        var activeOrderIds = activeDeliveries.Select(delivery => delivery.OrderId).ToHashSet();
        var orderResponses = orders
            .Select(order =>
            {
                if (activeOrderIds.Contains(order.Id))
                {
                    var activeDelivery = activeDeliveries.Single(delivery => delivery.OrderId == order.Id);
                    var state = _deliveryStateService.GetCurrentState(activeDelivery, utcNow);
                    return new OrderResponse(
                        order.Id,
                        order.CustomerName,
                        order.DestinationX,
                        order.DestinationY,
                        order.PackageWeightKg,
                        order.Priority,
                        state.OrderStatus,
                        order.QueueStatus,
                        order.QueuedAtUtc,
                        order.CreatedAt,
                        order.UpdatedAt);
                }

                if (deliveryByOrderId.TryGetValue(order.Id, out var completedDelivery) && completedDelivery.CompletedAtUtc <= utcNow)
                {
                    return new OrderResponse(
                        order.Id,
                        order.CustomerName,
                        order.DestinationX,
                        order.DestinationY,
                        order.PackageWeightKg,
                        order.Priority,
                        OrderStatus.Delivered,
                        OrderQueueStatus.Completed,
                        order.QueuedAtUtc,
                        order.CreatedAt,
                        order.UpdatedAt);
                }

                if (deliveryByOrderId.ContainsKey(order.Id))
                {
                    return new OrderResponse(
                        order.Id,
                        order.CustomerName,
                        order.DestinationX,
                        order.DestinationY,
                        order.PackageWeightKg,
                        order.Priority,
                        OrderStatus.InTransit,
                        OrderQueueStatus.Allocated,
                        order.QueuedAtUtc,
                        order.CreatedAt,
                        order.UpdatedAt);
                }

                return order.ToResponse();
            })
            .ToList();

        var deliveryResponses = activeDeliveries
            .Select(delivery =>
            {
                var state = _deliveryStateService.GetCurrentState(delivery, utcNow);
                return new DashboardDeliveryResponse(
                    delivery.Id,
                    delivery.OrderId,
                    delivery.DroneId,
                    delivery.Drone.Code,
                    state.DeliveryStatus,
                    state.CurrentPhase,
                    state.CurrentPhaseStartedAtUtc,
                    state.NextPhaseAtUtc,
                    state.CompletedAtUtc,
                    state.ElapsedSeconds,
                    state.RemainingPhaseSeconds,
                    state.ProgressPercentage,
                    delivery.EstimatedDistanceKm,
                    delivery.EstimatedBatteryConsumptionPercent,
                    delivery.DestinationX,
                    delivery.DestinationY);
            })
            .ToList();

        return new DashboardResponse(
            utcNow,
            completedDeliveries,
            pendingDeliveries,
            averageMinutes,
            mostEfficientDrone,
            droneResponses,
            orderResponses,
            deliveryResponses,
            plannedTrips.Select(trip => MapTrip(trip, utcNow)).ToList(),
            activeTrips.Select(trip => MapTrip(trip, utcNow)).ToList(),
            orderResponses.Where(order => order.QueueStatus == OrderQueueStatus.Queued).ToList());
    }

    private TripResponse MapTrip(Trip trip, DateTime utcNow)
    {
        var state = _tripStateService.GetCurrentState(trip, utcNow);
        var orders = trip.TripOrders
            .OrderBy(tripOrder => tripOrder.DeliverySequence)
            .Select(tripOrder => new TripOrderResponse(
                tripOrder.OrderId,
                tripOrder.Order.CustomerName,
                tripOrder.Order.Priority,
                tripOrder.Order.PackageWeightKg,
                tripOrder.Order.DestinationX,
                tripOrder.Order.DestinationY,
                tripOrder.DeliverySequence,
                tripOrder.EstimatedArrivalAtUtc))
            .ToList();

        return new TripResponse(
            trip.Id,
            trip.DroneId,
            trip.Drone.Code,
            state.TripStatus,
            state.CurrentPhase,
            trip.PlannedAtUtc,
            trip.LoadingStartedAtUtc,
            trip.FlyingStartedAtUtc,
            trip.DeliveringStartedAtUtc,
            trip.ReturningStartedAtUtc,
            trip.CompletedAtUtc,
            state.NextPhaseAtUtc,
            state.RemainingPhaseSeconds,
            state.ProgressPercentage,
            trip.TotalWeightKg,
            trip.Drone.MaxPackageWeightKg,
            Math.Round(trip.TotalWeightKg / trip.Drone.MaxPackageWeightKg * 100m, 2),
            trip.EstimatedDistanceKm,
            trip.EstimatedBatteryConsumptionPercentagePoints,
            trip.BatterySafetyMarginPercentagePoints,
            trip.MinimumRequiredBatteryPercentage,
            trip.BatteryAtDeparturePercentage,
            trip.ExpectedBatteryAtReturnPercentage,
            orders);
    }
}
