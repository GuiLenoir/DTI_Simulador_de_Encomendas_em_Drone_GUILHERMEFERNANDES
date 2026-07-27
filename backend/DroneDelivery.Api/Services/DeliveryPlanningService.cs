using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Api.Services;

public sealed class DeliveryPlanningService : IDeliveryPlanningService
{
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IDistanceService _distanceService;
    private readonly IRoutePlanningService _routePlanningService;
    private readonly ITripStateService _tripStateService;
    private readonly IChargingService _chargingService;
    private readonly IDroneOrderCapabilityService _droneOrderCapabilityService;
    private readonly IClock _clock;
    private readonly IDroneSettingsService _droneSettingsService;
    private readonly DroneDeliveryOptions _deliveryOptions;
    private readonly SimulationOptions _simulationOptions;
    private readonly ILogger<DeliveryPlanningService> _logger;

    public DeliveryPlanningService(
        DroneDeliveryDbContext dbContext,
        IDistanceService distanceService,
        IRoutePlanningService routePlanningService,
        ITripStateService tripStateService,
        IChargingService chargingService,
        IDroneOrderCapabilityService droneOrderCapabilityService,
        IClock clock,
        IDroneSettingsService droneSettingsService,
        IOptions<DroneDeliveryOptions> deliveryOptions,
        IOptions<SimulationOptions> simulationOptions,
        ILogger<DeliveryPlanningService> logger)
    {
        _dbContext = dbContext;
        _distanceService = distanceService;
        _routePlanningService = routePlanningService;
        _tripStateService = tripStateService;
        _chargingService = chargingService;
        _droneOrderCapabilityService = droneOrderCapabilityService;
        _clock = clock;
        _droneSettingsService = droneSettingsService;
        _deliveryOptions = deliveryOptions.Value;
        _simulationOptions = simulationOptions.Value;
        _logger = logger;
    }

    public async Task<DeliveryPlanningResponse> PlanAsync(CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => PlanCoreAsync(replanMutableTrips: true, includeNewPendingOrders: true, cancellationToken));
    }

    public async Task<DeliveryPlanningResponse> ProcessQueueAsync(CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => PlanCoreAsync(replanMutableTrips: false, includeNewPendingOrders: false, cancellationToken));
    }

    private async Task<DeliveryPlanningResponse> PlanCoreAsync(
        bool replanMutableTrips,
        bool includeNewPendingOrders,
        CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        _logger.LogDebug("Delivery planning started at {UtcNow}.", utcNow);
        await CompleteElapsedTripsAsync(utcNow, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (replanMutableTrips)
        {
            var mutableTrips = await _dbContext.Trips
                .Include(trip => trip.TripOrders)
                .ThenInclude(tripOrder => tripOrder.Order)
                .Where(trip => trip.Status == TripStatus.Planned && trip.LoadingStartedAtUtc > utcNow)
                .ToListAsync(cancellationToken);

            var releasedOrders = mutableTrips
                .SelectMany(trip => trip.TripOrders.Select(tripOrder => tripOrder.Order))
                .DistinctBy(order => order.Id)
                .ToList();

            foreach (var order in releasedOrders)
            {
                order.Status = OrderStatus.Pending;
                order.QueueStatus = OrderQueueStatus.Queued;
            }

            _dbContext.Trips.RemoveRange(mutableTrips);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var orderQuery = _dbContext.Orders
            .Where(order => order.Status == OrderStatus.Pending);
        orderQuery = includeNewPendingOrders
            ? orderQuery.Where(order => order.QueueStatus == OrderQueueStatus.Queued || order.QueueStatus == OrderQueueStatus.NotQueued)
            : orderQuery.Where(order => order.QueueStatus == OrderQueueStatus.Queued);

        var orders = await orderQuery
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.QueuedAtUtc ?? order.CreatedAt)
            .ThenByDescending(order => order.PackageWeightKg)
            .ThenBy(order => order.Id)
            .ToListAsync(cancellationToken);

        foreach (var order in orders.Where(order => order.QueueStatus == OrderQueueStatus.NotQueued))
        {
            order.QueueStatus = OrderQueueStatus.Queued;
            order.QueuedAtUtc = utcNow;
        }

        var drones = await LoadAvailableDronesAsync(utcNow, cancellationToken);
        var safetyMargin = (await _droneSettingsService.GetAsync(cancellationToken)).BatterySafetyMarginPercentagePoints;

        _logger.LogDebug("Planning {QueuedOrderCount} queued orders with {DroneCount} available drones.", orders.Count, drones.Count);

        var plan = await BuildBestPlanAsync(orders, drones, utcNow, safetyMargin, cancellationToken);
        var createdTrips = new List<Trip>();
        foreach (var candidate in plan.Trips)
        {
            var trip = CreateTrip(candidate, utcNow, safetyMargin);
            _dbContext.Trips.Add(trip);
            foreach (var order in candidate.Orders)
            {
                order.QueueStatus = OrderQueueStatus.Planned;
                order.Status = OrderStatus.Allocated;
            }

            createdTrips.Add(trip);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogDebug("Delivery planning generated {TripCount} trips and left {UnallocatedCount} orders queued.", createdTrips.Count, plan.UnallocatedOrders.Count);

        var trips = await GetTripsByIdsAsync(createdTrips.Select(trip => trip.Id).ToList(), utcNow, cancellationToken);
        return new DeliveryPlanningResponse(
            TripsCreated: trips.Count,
            OrdersAllocated: trips.Sum(trip => trip.Orders.Count),
            OrdersRemainingQueued: plan.UnallocatedOrders.Count,
            Trips: trips,
            UnallocatedOrders: plan.UnallocatedOrders);
    }

    public async Task<IReadOnlyList<TripResponse>> GetTripsAsync(CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        await CompleteElapsedTripsAsync(utcNow, cancellationToken);

        var trips = await _dbContext.Trips
            .Include(trip => trip.Drone)
            .Include(trip => trip.TripOrders)
            .ThenInclude(tripOrder => tripOrder.Order)
            .OrderByDescending(trip => trip.PlannedAtUtc)
            .ToListAsync(cancellationToken);

        return trips.Select(trip => MapTrip(trip, utcNow)).ToList();
    }

    public async Task<TripResponse> GetTripByIdAsync(int id, CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        await CompleteElapsedTripsAsync(utcNow, cancellationToken);

        var trip = await _dbContext.Trips
            .Include(item => item.Drone)
            .Include(item => item.TripOrders)
            .ThenInclude(item => item.Order)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Trip {id} was not found.");

        return MapTrip(trip, utcNow);
    }

    private async Task<List<Drone>> LoadAvailableDronesAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var activeDeliveryDroneIds = await _dbContext.Deliveries
            .Where(delivery => delivery.CompletedAtUtc > utcNow)
            .Select(delivery => delivery.DroneId)
            .ToListAsync(cancellationToken);
        var unavailableTripDroneIds = await _dbContext.Trips
            .Where(trip => trip.CompletedAtUtc > utcNow && trip.LoadingStartedAtUtc <= utcNow)
            .Select(trip => trip.DroneId)
            .ToListAsync(cancellationToken);

        var drones = await _dbContext.Drones
            .Where(drone => drone.IsActive)
            .Where(drone => !activeDeliveryDroneIds.Contains(drone.Id))
            .Where(drone => !unavailableTripDroneIds.Contains(drone.Id))
            .OrderByDescending(drone => drone.MaxPackageWeightKg)
            .ThenByDescending(drone => drone.BatteryLevelPercent)
            .ThenBy(drone => drone.Id)
            .ToListAsync(cancellationToken);

        var available = new List<Drone>();
        foreach (var drone in drones)
        {
            var runtime = _chargingService.GetCurrentState(drone, utcNow);
            if (runtime.Status == DroneStatus.Charging)
            {
                if (await _droneOrderCapabilityService.CanServeAnyPendingOrderAsync(drone, runtime.BatteryLevelPercent, cancellationToken))
                {
                    StopCharging(drone, runtime.BatteryLevelPercent);
                    available.Add(drone);
                }

                continue;
            }

            if (runtime.Status != DroneStatus.Idle)
            {
                continue;
            }

            if (drone.Status == DroneStatus.Charging && drone.ChargingCompletedAtUtc is not null && utcNow >= drone.ChargingCompletedAtUtc)
            {
                StopCharging(drone, runtime.BatteryLevelPercent);
            }

            available.Add(drone);
        }

        return available;
    }

    private static void StopCharging(Drone drone, decimal batteryLevelPercent)
    {
        drone.Status = DroneStatus.Idle;
        drone.BatteryLevelPercent = Math.Clamp(Math.Round(batteryLevelPercent, 2), 0m, 100m);
        drone.ChargingStartedAtUtc = null;
        drone.BatteryAtChargingStartPercentage = null;
        drone.ChargingCompletedAtUtc = null;
    }

    private async Task<PlanningCandidate> BuildBestPlanAsync(
        IReadOnlyList<DeliveryOrder> orders,
        IReadOnlyList<Drone> drones,
        DateTime utcNow,
        decimal safetyMargin,
        CancellationToken cancellationToken)
    {
        var orderedOrders = orders.ToList();
        var orderedDrones = drones
            .OrderBy(drone => drone.MaxPackageWeightKg)
            .ThenBy(drone => drone.MaxRangeKm)
            .ThenBy(drone => drone.Id)
            .ToList();
        var best = await SearchBestPlanAsync(orderedOrders, orderedDrones, utcNow, safetyMargin, cancellationToken);
        return best;
    }

    private async Task<PlanningCandidate> SearchBestPlanAsync(
        IReadOnlyList<DeliveryOrder> remainingOrders,
        IReadOnlyList<Drone> availableDrones,
        DateTime utcNow,
        decimal safetyMargin,
        CancellationToken cancellationToken)
    {
        if (remainingOrders.Count == 0 || availableDrones.Count == 0)
        {
            return new PlanningCandidate(
                Trips: new List<TripCandidate>(),
                UnallocatedOrders: remainingOrders
                    .Select(order => new UnallocatedOrderResponse(order.Id, order.CustomerName, "NO_VALID_DRONE_AVAILABLE"))
                    .ToList());
        }

        var seedOrder = remainingOrders[0];
        var candidates = await BuildTripCandidatesAsync(seedOrder, remainingOrders, availableDrones, utcNow, safetyMargin, cancellationToken);
        if (candidates.Count == 0)
        {
            var rest = await SearchBestPlanAsync(remainingOrders.Skip(1).ToList(), availableDrones, utcNow, safetyMargin, cancellationToken);
            return rest with
            {
                UnallocatedOrders = new[] { new UnallocatedOrderResponse(seedOrder.Id, seedOrder.CustomerName, "NO_VALID_DRONE_AVAILABLE") }
                    .Concat(rest.UnallocatedOrders)
                    .ToList()
            };
        }

        PlanningCandidate? best = null;
        foreach (var candidate in candidates)
        {
            var nextOrders = remainingOrders
                .Where(order => candidate.Orders.All(selected => selected.Id != order.Id))
                .ToList();
            var nextDrones = availableDrones
                .Where(drone => drone.Id != candidate.Drone.Id)
                .ToList();
            var rest = await SearchBestPlanAsync(nextOrders, nextDrones, utcNow, safetyMargin, cancellationToken);
            var plan = rest with
            {
                Trips = new[] { candidate }.Concat(rest.Trips).ToList()
            };

            if (best is null || ComparePlans(plan, best, remainingOrders) < 0)
            {
                best = plan;
            }
        }

        return best!;
    }

    private async Task<IReadOnlyList<TripCandidate>> BuildTripCandidatesAsync(
        DeliveryOrder seedOrder,
        IReadOnlyList<DeliveryOrder> availableOrders,
        IReadOnlyList<Drone> availableDrones,
        DateTime utcNow,
        decimal safetyMargin,
        CancellationToken cancellationToken)
    {
        var candidates = new List<TripCandidate>();
        foreach (var drone in availableDrones)
        {
            foreach (var orderSet in BuildOrderSubsets(seedOrder, availableOrders, drone.MaxPackageWeightKg))
            {
                var candidate = await BuildCandidateForOrdersAsync(drone, orderSet, utcNow, safetyMargin, cancellationToken);
                if (candidate is not null)
                {
                    candidates.Add(candidate);
                }
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.Orders.Count)
            .ThenBy(candidate => candidate.RouteDistanceKm)
            .ThenBy(candidate => candidate.Drone.MaxPackageWeightKg)
            .ThenBy(candidate => candidate.Drone.Id)
            .ThenBy(candidate => string.Join(",", candidate.Orders.Select(order => order.Id)))
            .ToList();
    }

    private async Task<TripCandidate?> BuildCandidateForOrdersAsync(
        Drone drone,
        IReadOnlyList<DeliveryOrder> orders,
        DateTime utcNow,
        decimal safetyMargin,
        CancellationToken cancellationToken)
    {
        var totalWeight = orders.Sum(order => order.PackageWeightKg);
        if (totalWeight > drone.MaxPackageWeightKg)
        {
            return null;
        }

        var stops = BuildDeliverySequence(orders);
        var routeDistance = await TryCalculateRouteDistanceAsync(stops, cancellationToken);
        if (routeDistance is null)
        {
            return null;
        }

        var estimatedConsumption = CalculateBatteryConsumption(drone, routeDistance.Value);
        var minimumRequiredBattery = estimatedConsumption + safetyMargin;
        if (routeDistance.Value > drone.MaxRangeKm ||
            minimumRequiredBattery > 100m ||
            drone.BatteryLevelPercent < minimumRequiredBattery)
        {
            return null;
        }

        return new TripCandidate(
            drone,
            stops,
            routeDistance.Value,
            estimatedConsumption,
            minimumRequiredBattery,
            totalWeight / drone.MaxPackageWeightKg * 100m,
            utcNow);
    }

    private static IReadOnlyList<IReadOnlyList<DeliveryOrder>> BuildOrderSubsets(
        DeliveryOrder seedOrder,
        IReadOnlyList<DeliveryOrder> availableOrders,
        decimal maxWeight)
    {
        var optionalOrders = availableOrders
            .Where(order => order.Id != seedOrder.Id)
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.QueuedAtUtc ?? order.CreatedAt)
            .ThenByDescending(order => order.PackageWeightKg)
            .ThenBy(order => order.Id)
            .ToList();
        var subsets = new List<IReadOnlyList<DeliveryOrder>>();

        void Search(int index, List<DeliveryOrder> selected, decimal weight)
        {
            if (index == optionalOrders.Count)
            {
                subsets.Add(selected.ToList());
                return;
            }

            Search(index + 1, selected, weight);

            var order = optionalOrders[index];
            if (weight + order.PackageWeightKg <= maxWeight)
            {
                selected.Add(order);
                Search(index + 1, selected, weight + order.PackageWeightKg);
                selected.RemoveAt(selected.Count - 1);
            }
        }

        if (seedOrder.PackageWeightKg > maxWeight)
        {
            return Array.Empty<IReadOnlyList<DeliveryOrder>>();
        }

        Search(0, new List<DeliveryOrder> { seedOrder }, seedOrder.PackageWeightKg);
        return subsets
            .OrderByDescending(subset => subset.Count)
            .ThenByDescending(subset => subset.Sum(order => order.PackageWeightKg))
            .ThenBy(subset => string.Join(",", subset.Select(order => order.Id).OrderBy(id => id)))
            .ToList();
    }

    private static int ComparePlans(PlanningCandidate left, PlanningCandidate right, IReadOnlyList<DeliveryOrder> allOrders)
    {
        var highestPriority = allOrders.Count == 0 ? OrderPriority.Low : allOrders.Max(order => order.Priority);
        return left.UnallocatedOrders.Count.CompareTo(right.UnallocatedOrders.Count) is var unallocated && unallocated != 0 ? unallocated :
            left.Trips.Count.CompareTo(right.Trips.Count) is var trips && trips != 0 ? trips :
            CountHighestPriorityInFirstTrip(right, highestPriority).CompareTo(CountHighestPriorityInFirstTrip(left, highestPriority)) is var priority && priority != 0 ? priority :
            left.Trips.Sum(trip => trip.RouteDistanceKm).CompareTo(right.Trips.Sum(trip => trip.RouteDistanceKm)) is var distance && distance != 0 ? distance :
            right.Trips.Sum(trip => trip.CapacityUsagePercentage).CompareTo(left.Trips.Sum(trip => trip.CapacityUsagePercentage)) is var usage && usage != 0 ? usage :
            left.Trips.Sum(trip => trip.Drone.MaxPackageWeightKg).CompareTo(right.Trips.Sum(trip => trip.Drone.MaxPackageWeightKg)) is var droneSize && droneSize != 0 ? droneSize :
            string.Join("|", left.Trips.Select(trip => $"{trip.Drone.Id}:{string.Join(",", trip.Orders.Select(order => order.Id))}"))
                .CompareTo(string.Join("|", right.Trips.Select(trip => $"{trip.Drone.Id}:{string.Join(",", trip.Orders.Select(order => order.Id))}")));
    }

    private static int CountHighestPriorityInFirstTrip(PlanningCandidate plan, OrderPriority highestPriority) =>
        plan.Trips.FirstOrDefault()?.Orders.Count(order => order.Priority == highestPriority) ?? 0;

    private Trip CreateTrip(TripCandidate candidate, DateTime utcNow, decimal safetyMargin)
    {
        var outboundSeconds = CalculateFlightDurationSeconds(candidate.RouteDistanceKm);
        var loadingStartedAtUtc = utcNow.AddSeconds(1);
        var flyingStartedAtUtc = loadingStartedAtUtc.AddSeconds(_simulationOptions.LoadingDurationSeconds);
        var deliveringStartedAtUtc = flyingStartedAtUtc.AddSeconds(outboundSeconds);
        var returningStartedAtUtc = deliveringStartedAtUtc.AddSeconds(_simulationOptions.DeliveryDurationSeconds * candidate.Orders.Count);
        var completedAtUtc = returningStartedAtUtc.AddSeconds(1);

        var trip = new Trip
        {
            DroneId = candidate.Drone.Id,
            Status = TripStatus.Planned,
            PlannedAtUtc = utcNow,
            LoadingStartedAtUtc = loadingStartedAtUtc,
            FlyingStartedAtUtc = flyingStartedAtUtc,
            DeliveringStartedAtUtc = deliveringStartedAtUtc,
            ReturningStartedAtUtc = returningStartedAtUtc,
            CompletedAtUtc = completedAtUtc,
            TotalWeightKg = candidate.Orders.Sum(order => order.PackageWeightKg),
            EstimatedDistanceKm = candidate.RouteDistanceKm,
            EstimatedBatteryConsumptionPercentagePoints = candidate.EstimatedBatteryConsumptionPercentagePoints,
            BatterySafetyMarginPercentagePoints = safetyMargin,
            MinimumRequiredBatteryPercentage = candidate.MinimumRequiredBatteryPercentage,
            BatteryAtDeparturePercentage = candidate.Drone.BatteryLevelPercent,
            ExpectedBatteryAtReturnPercentage = Math.Max(0, candidate.Drone.BatteryLevelPercent - candidate.EstimatedBatteryConsumptionPercentagePoints),
            LoadingDurationSeconds = _simulationOptions.LoadingDurationSeconds,
            OutboundFlightDurationSeconds = outboundSeconds,
            DeliveryDurationSeconds = _simulationOptions.DeliveryDurationSeconds * candidate.Orders.Count,
            ReturnFlightDurationSeconds = 1
        };

        var sequence = 1;
        var arrival = deliveringStartedAtUtc;
        foreach (var order in candidate.Orders)
        {
            trip.TripOrders.Add(new TripOrder
            {
                OrderId = order.Id,
                DeliverySequence = sequence++,
                EstimatedArrivalAtUtc = arrival,
                DeliveryStartedAtUtc = arrival,
                DeliveryCompletedAtUtc = arrival.AddSeconds(_simulationOptions.DeliveryDurationSeconds)
            });
            arrival = arrival.AddSeconds(_simulationOptions.DeliveryDurationSeconds);
        }

        return trip;
    }

    private IReadOnlyList<DeliveryOrder> BuildDeliverySequence(IReadOnlyList<DeliveryOrder> orders)
    {
        var result = new List<DeliveryOrder>();
        var remaining = orders
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.QueuedAtUtc)
            .ThenBy(order => order.Id)
            .ToList();
        var currentX = _deliveryOptions.BaseX;
        var currentY = _deliveryOptions.BaseY;

        while (remaining.Count > 0)
        {
            var highestPriority = remaining.Max(order => order.Priority);
            var samePriority = remaining.Where(order => order.Priority == highestPriority).ToList();
            var next = samePriority
                .OrderBy(order => _distanceService.Calculate(currentX, currentY, order.DestinationX, order.DestinationY))
                .ThenBy(order => order.QueuedAtUtc)
                .ThenBy(order => order.Id)
                .First();
            result.Add(next);
            remaining.Remove(next);
            currentX = next.DestinationX;
            currentY = next.DestinationY;
        }

        return result;
    }

    private async Task<decimal?> TryCalculateRouteDistanceAsync(IReadOnlyList<DeliveryOrder> orderedStops, CancellationToken cancellationToken)
    {
        var start = new RoutePoint(_deliveryOptions.BaseX, _deliveryOptions.BaseY);
        var stops = orderedStops
            .Select(order => new RoutePoint(order.DestinationX, order.DestinationY))
            .ToList();
        var end = _deliveryOptions.RequireReturnToBase && stops.Count > 0
            ? new RoutePoint(_deliveryOptions.BaseX, _deliveryOptions.BaseY)
            : stops.LastOrDefault() ?? start;
        try
        {
            return await _routePlanningService.CalculateDistanceAsync(start, stops, end, cancellationToken);
        }
        catch (ValidationException exception) when (exception.Code is "ROUTE_BLOCKED_BY_NO_FLY_ZONE" or "NO_VALID_ROUTE_AVAILABLE")
        {
            return null;
        }
    }

    private decimal CalculateBatteryConsumption(Drone drone, decimal distanceKm) =>
        Math.Round(distanceKm * drone.BatteryConsumptionPercentagePerKm, 2);

    private int CalculateFlightDurationSeconds(decimal distanceKm) =>
        Math.Max(1, (int)Math.Ceiling(distanceKm * _simulationOptions.SecondsPerKilometer));

    private async Task CompleteElapsedTripsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var elapsedTrips = await _dbContext.Trips
            .Include(trip => trip.Drone)
            .Include(trip => trip.TripOrders)
            .ThenInclude(tripOrder => tripOrder.Order)
            .Where(trip => trip.CompletedAtUtc <= utcNow && trip.Status != TripStatus.Completed)
            .ToListAsync(cancellationToken);

        foreach (var trip in elapsedTrips)
        {
            trip.Status = TripStatus.Completed;
            trip.Drone.CurrentX = _deliveryOptions.BaseX;
            trip.Drone.CurrentY = _deliveryOptions.BaseY;
            await ApplyPostTripBatteryStateAsync(trip.Drone, trip.ExpectedBatteryAtReturnPercentage, trip.CompletedAtUtc, cancellationToken);

            foreach (var tripOrder in trip.TripOrders)
            {
                tripOrder.Order.Status = OrderStatus.Delivered;
                tripOrder.Order.QueueStatus = OrderQueueStatus.Completed;
            }
        }

        if (elapsedTrips.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ApplyPostTripBatteryStateAsync(Drone drone, decimal batteryAtReturnPercentage, DateTime completedAtUtc, CancellationToken cancellationToken)
    {
        var battery = Math.Clamp(batteryAtReturnPercentage, 0m, 100m);
        if (battery >= 100m || await _droneOrderCapabilityService.CanServeAnyPendingOrderAsync(drone, battery, cancellationToken))
        {
            drone.Status = DroneStatus.Idle;
            drone.BatteryLevelPercent = battery;
            drone.ChargingStartedAtUtc = null;
            drone.BatteryAtChargingStartPercentage = null;
            drone.ChargingCompletedAtUtc = null;
            return;
        }

        _chargingService.StartChargingIfNeeded(drone, battery, completedAtUtc);
    }

    private async Task<IReadOnlyList<TripResponse>> GetTripsByIdsAsync(IReadOnlyList<int> tripIds, DateTime utcNow, CancellationToken cancellationToken)
    {
        var trips = await _dbContext.Trips
            .Include(trip => trip.Drone)
            .Include(trip => trip.TripOrders)
            .ThenInclude(tripOrder => tripOrder.Order)
            .Where(trip => tripIds.Contains(trip.Id))
            .OrderBy(trip => trip.Id)
            .ToListAsync(cancellationToken);

        return trips.Select(trip => MapTrip(trip, utcNow)).ToList();
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

    private sealed record TripCandidate(
        Drone Drone,
        IReadOnlyList<DeliveryOrder> Orders,
        decimal RouteDistanceKm,
        decimal EstimatedBatteryConsumptionPercentagePoints,
        decimal MinimumRequiredBatteryPercentage,
        decimal CapacityUsagePercentage,
        DateTime PlannedAtUtc);

    private sealed record PlanningCandidate(
        IReadOnlyList<TripCandidate> Trips,
        IReadOnlyList<UnallocatedOrderResponse> UnallocatedOrders);
}
