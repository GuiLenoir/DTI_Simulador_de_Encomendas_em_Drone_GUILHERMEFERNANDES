using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Api.Services;

public sealed class UpcomingTripService : IUpcomingTripService
{
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IRoutePlanningService _routePlanningService;
    private readonly IChargingService _chargingService;
    private readonly IDroneSettingsService _settingsService;
    private readonly IClock _clock;
    private readonly DroneDeliveryOptions _options;

    public UpcomingTripService(
        DroneDeliveryDbContext dbContext,
        IRoutePlanningService routePlanningService,
        IChargingService chargingService,
        IDroneSettingsService settingsService,
        IClock clock,
        IOptions<DroneDeliveryOptions> options)
    {
        _dbContext = dbContext;
        _routePlanningService = routePlanningService;
        _chargingService = chargingService;
        _settingsService = settingsService;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<UpcomingTripsResponse> GetAsync(CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var plannedTrips = await _dbContext.Trips
            .Include(trip => trip.Drone)
            .Include(trip => trip.TripOrders)
            .ThenInclude(tripOrder => tripOrder.Order)
            .Where(trip => trip.Status == TripStatus.Planned && trip.LoadingStartedAtUtc > utcNow)
            .OrderBy(trip => trip.Drone.Code)
            .ThenBy(trip => trip.LoadingStartedAtUtc)
            .ThenBy(trip => trip.Id)
            .ToListAsync(cancellationToken);

        var activeTrips = await _dbContext.Trips
            .Where(trip => trip.CompletedAtUtc > utcNow && trip.LoadingStartedAtUtc <= utcNow)
            .ToListAsync(cancellationToken);
        var activeByDroneId = activeTrips
            .GroupBy(trip => trip.DroneId)
            .ToDictionary(group => group.Key, group => group.OrderBy(trip => trip.CompletedAtUtc).First());

        var upcoming = plannedTrips.Select(trip => MapTrip(trip, utcNow, activeByDroneId)).ToList();
        var plannedOrderIds = plannedTrips.SelectMany(trip => trip.TripOrders.Select(tripOrder => tripOrder.OrderId)).ToHashSet();
        var unplannedOrders = await _dbContext.Orders
            .Where(order => order.Status == OrderStatus.Pending)
            .Where(order => order.QueueStatus == OrderQueueStatus.Queued || order.QueueStatus == OrderQueueStatus.NotQueued)
            .Where(order => !plannedOrderIds.Contains(order.Id))
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.QueuedAtUtc ?? order.CreatedAt)
            .ThenBy(order => order.Id)
            .ToListAsync(cancellationToken);

        var unplanned = new List<UnplannedOrderResponse>();
        foreach (var order in unplannedOrders)
        {
            var reason = await GetUnplannedReasonAsync(order, utcNow, cancellationToken);
            unplanned.Add(new UnplannedOrderResponse(
                order.Id,
                $"PED-{order.Id}",
                order.CustomerName,
                order.Priority,
                order.PackageWeightKg,
                order.QueuedAtUtc,
                reason.Code,
                reason.Message));
        }

        return new UpcomingTripsResponse(utcNow, upcoming, unplanned);
    }

    private UpcomingTripResponse MapTrip(Trip trip, DateTime utcNow, IReadOnlyDictionary<int, Trip> activeByDroneId)
    {
        var blockingTrip = activeByDroneId.GetValueOrDefault(trip.DroneId);
        var runtime = _chargingService.GetCurrentState(trip.Drone, utcNow);
        var waiting = blockingTrip is not null
            ? ("WAITING_FOR_ACTIVE_TRIP", "Aguardando drone retornar", "Inicio previsto apos a viagem ativa")
            : runtime.Status == DroneStatus.Charging
                ? ("WAITING_FOR_CHARGE", "Aguardando recarga", "Drone em recarga antes da proxima viagem")
                : ("WAITING_TO_START", "Aguardando inicio", "Viagem pronta para iniciar");

        var orders = trip.TripOrders
            .OrderBy(tripOrder => tripOrder.DeliverySequence)
            .Select(tripOrder => new UpcomingTripOrderResponse(
                tripOrder.OrderId,
                $"PED-{tripOrder.OrderId}",
                tripOrder.Order.CustomerName,
                tripOrder.Order.Priority,
                tripOrder.Order.PackageWeightKg))
            .ToList();

        return new UpcomingTripResponse(
            trip.Id,
            trip.Drone.Code,
            orders,
            trip.TotalWeightKg,
            trip.Drone.MaxPackageWeightKg,
            Math.Round(trip.TotalWeightKg / trip.Drone.MaxPackageWeightKg * 100m, 2),
            trip.EstimatedDistanceKm,
            trip.EstimatedBatteryConsumptionPercentagePoints,
            trip.BatterySafetyMarginPercentagePoints,
            trip.MinimumRequiredBatteryPercentage,
            blockingTrip?.CompletedAtUtc ?? trip.LoadingStartedAtUtc,
            waiting.Item1,
            waiting.Item2,
            waiting.Item3,
            blockingTrip?.Id);
    }

    private async Task<(string Code, string Message)> GetUnplannedReasonAsync(DeliveryOrder order, DateTime utcNow, CancellationToken cancellationToken)
    {
        var drones = await _dbContext.Drones
            .Where(drone => drone.IsActive)
            .OrderBy(drone => drone.MaxPackageWeightKg)
            .ThenBy(drone => drone.Id)
            .ToListAsync(cancellationToken);

        if (drones.Count == 0)
        {
            return ("WAITING_FOR_DRONE", "Nenhum drone esta disponivel no momento.");
        }

        var capableByWeight = drones.Where(drone => drone.MaxPackageWeightKg >= order.PackageWeightKg).ToList();
        if (capableByWeight.Count == 0)
        {
            return ("WAITING_FOR_CAPACITY", "Nenhum drone possui capacidade suficiente.");
        }

        var routeDistances = new Dictionary<int, decimal>();
        foreach (var drone in capableByWeight)
        {
            var distance = await TryCalculateRouteDistanceAsync(order, cancellationToken);
            if (distance is not null)
            {
                routeDistances[drone.Id] = distance.Value;
            }
        }

        if (routeDistances.Count == 0)
        {
            return ("WAITING_FOR_ROUTE", "A rota esta bloqueada por uma zona de exclusao.");
        }

        var withinRange = capableByWeight.Where(drone => routeDistances[drone.Id] <= drone.MaxRangeKm).ToList();
        if (withinRange.Count == 0)
        {
            return ("WAITING_FOR_ROUTE", "Nenhum drone possui alcance suficiente para essa rota.");
        }

        var safetyMargin = (await _settingsService.GetAsync(cancellationToken)).BatterySafetyMarginPercentagePoints;
        var hasBattery = withinRange.Any(drone =>
            _chargingService.GetCurrentState(drone, utcNow).BatteryLevelPercent >=
            Math.Round(routeDistances[drone.Id] * drone.BatteryConsumptionPercentagePerKm, 2) + safetyMargin);
        if (!hasBattery)
        {
            return ("WAITING_FOR_CHARGE", "A bateria disponivel ainda nao e suficiente.");
        }

        return ("WAITING_FOR_REPLANNING", "Aguardando replanejamento.");
    }

    private async Task<decimal?> TryCalculateRouteDistanceAsync(DeliveryOrder order, CancellationToken cancellationToken)
    {
        try
        {
            return await _routePlanningService.CalculateDistanceAsync(
                new RoutePoint(_options.BaseX, _options.BaseY),
                new[] { new RoutePoint(order.DestinationX, order.DestinationY) },
                _options.RequireReturnToBase ? new RoutePoint(_options.BaseX, _options.BaseY) : new RoutePoint(order.DestinationX, order.DestinationY),
                cancellationToken);
        }
        catch (ValidationException exception) when (exception.Code is "ROUTE_BLOCKED_BY_NO_FLY_ZONE" or "NO_VALID_ROUTE_AVAILABLE")
        {
            return null;
        }
    }
}
