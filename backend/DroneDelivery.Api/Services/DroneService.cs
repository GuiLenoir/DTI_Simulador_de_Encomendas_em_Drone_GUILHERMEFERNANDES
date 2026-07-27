using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneDelivery.Api.Services;

public sealed class DroneService : IDroneService
{
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IDeliveryStateService _deliveryStateService;
    private readonly ITripStateService _tripStateService;
    private readonly IChargingService _chargingService;
    private readonly IClock _clock;
    private readonly IDroneSettingsService _settingsService;
    private readonly IDeliveryPlanningService _deliveryPlanningService;
    private readonly IDeliveryService _deliveryService;

    public DroneService(
        DroneDeliveryDbContext dbContext,
        IDeliveryStateService deliveryStateService,
        ITripStateService tripStateService,
        IChargingService chargingService,
        IClock clock,
        IDroneSettingsService settingsService,
        IDeliveryPlanningService deliveryPlanningService,
        IDeliveryService deliveryService)
    {
        _dbContext = dbContext;
        _deliveryStateService = deliveryStateService;
        _tripStateService = tripStateService;
        _chargingService = chargingService;
        _clock = clock;
        _settingsService = settingsService;
        _deliveryPlanningService = deliveryPlanningService;
        _deliveryService = deliveryService;
    }

    public async Task<IReadOnlyList<DroneResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _deliveryService.CompleteElapsedAsync(cancellationToken);
        await _deliveryPlanningService.ProcessQueueAsync(cancellationToken);
        var utcNow = _clock.UtcNow;
        var settings = await _settingsService.GetAsync(cancellationToken);
        var drones = await _dbContext.Drones
            .OrderBy(drone => drone.Code)
            .ToListAsync(cancellationToken);
        var executingDeliveryIds = await _dbContext.Deliveries
            .Where(delivery => delivery.CompletedAtUtc > utcNow)
            .Select(delivery => delivery.DroneId)
            .ToListAsync(cancellationToken);
        var executingTripIds = await _dbContext.Trips
            .Where(trip => trip.CompletedAtUtc > utcNow && trip.LoadingStartedAtUtc <= utcNow)
            .Select(trip => trip.DroneId)
            .ToListAsync(cancellationToken);
        var plannedTripIds = await _dbContext.Trips
            .Where(trip => trip.Status == TripStatus.Planned && trip.LoadingStartedAtUtc > utcNow)
            .Select(trip => trip.DroneId)
            .ToListAsync(cancellationToken);

        return drones
            .Select(drone => MapResponse(
                drone,
                settings.BatterySafetyMarginPercentagePoints,
                executingDeliveryIds.Contains(drone.Id) || executingTripIds.Contains(drone.Id),
                plannedTripIds.Contains(drone.Id),
                utcNow))
            .ToList();
    }

    public async Task<DroneResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await _deliveryService.CompleteElapsedAsync(cancellationToken);
        await _deliveryPlanningService.ProcessQueueAsync(cancellationToken);
        var utcNow = _clock.UtcNow;
        var settings = await _settingsService.GetAsync(cancellationToken);
        var drone = await FindAsync(id, cancellationToken);
        var hasExecuting = await HasExecutingTripAsync(drone.Id, utcNow, cancellationToken);
        var hasPlanned = await HasPlannedTripsAsync(drone.Id, utcNow, cancellationToken);
        return MapResponse(drone, settings.BatterySafetyMarginPercentagePoints, hasExecuting, hasPlanned, utcNow);
    }

    public async Task<DroneResponse> CreateAsync(CreateDroneRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        await EnsureCodeIsUniqueAsync(request.Code, ignoredDroneId: null, cancellationToken);

        var drone = new Drone
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            MaxPackageWeightKg = request.MaxPackageWeightKg,
            MaxRangeKm = request.MaxRangeKm,
            BatteryLevelPercent = request.BatteryLevelPercent,
            AverageSpeedKmPerHour = request.AverageSpeedKmPerHour,
            BatteryConsumptionPercentagePerKm = request.BatteryConsumptionPercentagePerKm,
            CurrentX = request.CurrentX,
            CurrentY = request.CurrentY,
            Status = request.Status,
            Notes = NormalizeNotes(request.Notes),
            IsActive = request.IsActive
        };

        _dbContext.Drones.Add(drone);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(drone.Id, cancellationToken);
    }

    public async Task<DroneResponse> UpdateAsync(int id, UpdateDroneRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        await EnsureCodeIsUniqueAsync(request.Code, id, cancellationToken);

        var utcNow = _clock.UtcNow;
        var drone = await FindAsync(id, cancellationToken);
        var operationalChange = HasOperationalChange(drone, request);
        if (operationalChange && await HasExecutingTripAsync(drone.Id, utcNow, cancellationToken))
        {
            throw new ValidationException("DRONE_IS_EXECUTING_TRIP", "Drone is executing trip", "Operational drone data cannot be changed while a trip is executing.");
        }

        if (operationalChange && await HasPlannedTripsAsync(drone.Id, utcNow, cancellationToken))
        {
            await CancelPlannedTripsAsync(drone.Id, utcNow, cancellationToken);
        }

        Apply(drone, request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (operationalChange)
        {
            await _deliveryPlanningService.ProcessQueueAsync(cancellationToken);
        }

        return await GetByIdAsync(drone.Id, cancellationToken);
    }

    public async Task<DroneResponse> ActivateAsync(int id, CancellationToken cancellationToken)
    {
        var drone = await FindAsync(id, cancellationToken);
        drone.IsActive = true;
        if (drone.Status == DroneStatus.Unavailable)
        {
            drone.Status = DroneStatus.Idle;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _deliveryPlanningService.ProcessQueueAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<DroneResponse> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var drone = await FindAsync(id, cancellationToken);
        if (await HasExecutingTripAsync(drone.Id, utcNow, cancellationToken))
        {
            throw new ValidationException("DRONE_IS_EXECUTING_TRIP", "Drone is executing trip", "Drone cannot be deactivated while a trip is executing.");
        }

        if (await HasPlannedTripsAsync(drone.Id, utcNow, cancellationToken))
        {
            await CancelPlannedTripsAsync(drone.Id, utcNow, cancellationToken);
        }

        drone.IsActive = false;
        drone.Status = DroneStatus.Unavailable;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _deliveryPlanningService.ProcessQueueAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken) =>
        DeactivateAsync(id, cancellationToken);

    private async Task<Drone> FindAsync(int id, CancellationToken cancellationToken) =>
        await _dbContext.Drones.FirstOrDefaultAsync(drone => drone.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Drone {id} was not found.");

    private async Task EnsureCodeIsUniqueAsync(string code, int? ignoredDroneId, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim();
        var exists = await _dbContext.Drones
            .AnyAsync(drone => drone.Code == normalizedCode && (!ignoredDroneId.HasValue || drone.Id != ignoredDroneId.Value), cancellationToken);
        if (exists)
        {
            throw new ValidationException("DRONE_CODE_ALREADY_EXISTS", "Drone code already exists", "Drone code must be unique.");
        }
    }

    private async Task<bool> HasExecutingTripAsync(int droneId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var hasActiveDelivery = await _dbContext.Deliveries
            .AnyAsync(delivery => delivery.DroneId == droneId && delivery.CompletedAtUtc > utcNow, cancellationToken);
        var hasActiveTrip = await _dbContext.Trips
            .AnyAsync(trip => trip.DroneId == droneId && trip.CompletedAtUtc > utcNow && trip.LoadingStartedAtUtc <= utcNow, cancellationToken);
        return hasActiveDelivery || hasActiveTrip;
    }

    private async Task<bool> HasPlannedTripsAsync(int droneId, DateTime utcNow, CancellationToken cancellationToken) =>
        await _dbContext.Trips
            .AnyAsync(trip => trip.DroneId == droneId && trip.Status == TripStatus.Planned && trip.LoadingStartedAtUtc > utcNow, cancellationToken);

    private async Task CancelPlannedTripsAsync(int droneId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var trips = await _dbContext.Trips
            .Include(trip => trip.TripOrders)
            .ThenInclude(tripOrder => tripOrder.Order)
            .Where(trip => trip.DroneId == droneId && trip.Status == TripStatus.Planned && trip.LoadingStartedAtUtc > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var order in trips.SelectMany(trip => trip.TripOrders.Select(tripOrder => tripOrder.Order)).DistinctBy(order => order.Id))
        {
            order.Status = OrderStatus.Pending;
            order.QueueStatus = OrderQueueStatus.Queued;
            order.QueuedAtUtc ??= utcNow;
        }

        _dbContext.Trips.RemoveRange(trips);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(CreateDroneRequest request) =>
        ValidateCommon(request.Code, request.Name, request.MaxPackageWeightKg, request.MaxRangeKm, request.BatteryLevelPercent,
            request.AverageSpeedKmPerHour, request.BatteryConsumptionPercentagePerKm, request.Status);

    private static void Validate(UpdateDroneRequest request) =>
        ValidateCommon(request.Code, request.Name, request.MaxPackageWeightKg, request.MaxRangeKm, request.BatteryLevelPercent,
            request.AverageSpeedKmPerHour, request.BatteryConsumptionPercentagePerKm, request.Status);

    private static void ValidateCommon(
        string code,
        string name,
        decimal maxPackageWeightKg,
        decimal maxRangeKm,
        decimal batteryLevelPercent,
        decimal averageSpeedKmPerHour,
        decimal batteryConsumptionPercentagePerKm,
        DroneStatus status)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ValidationException("INVALID_DRONE_CODE", "Invalid drone code", "Drone code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("INVALID_DRONE_NAME", "Invalid drone name", "Drone name is required.");
        }

        if (maxPackageWeightKg <= 0)
        {
            throw new ValidationException("INVALID_DRONE_CAPACITY", "Invalid drone capacity", "Drone capacity must be greater than zero.");
        }

        if (maxRangeKm <= 0)
        {
            throw new ValidationException("INVALID_DRONE_RANGE", "Invalid drone range", "Drone range must be greater than zero.");
        }

        if (batteryLevelPercent is < 0 or > 100)
        {
            throw new ValidationException("INVALID_BATTERY_PERCENTAGE", "Invalid battery percentage", "Battery level must be between 0 and 100.");
        }

        if (averageSpeedKmPerHour <= 0 || batteryConsumptionPercentagePerKm <= 0)
        {
            throw new ValidationException("INVALID_DRONE_CAPACITY", "Invalid drone operation data", "Drone speed and battery consumption must be greater than zero.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ValidationException("INVALID_DRONE_STATUS", "Invalid drone status", "Drone status is invalid.");
        }
    }

    private static bool HasOperationalChange(Drone drone, UpdateDroneRequest request) =>
        drone.Code != request.Code.Trim() ||
        drone.MaxPackageWeightKg != request.MaxPackageWeightKg ||
        drone.MaxRangeKm != request.MaxRangeKm ||
        drone.BatteryLevelPercent != request.BatteryLevelPercent ||
        drone.AverageSpeedKmPerHour != request.AverageSpeedKmPerHour ||
        drone.BatteryConsumptionPercentagePerKm != request.BatteryConsumptionPercentagePerKm ||
        drone.CurrentX != request.CurrentX ||
        drone.CurrentY != request.CurrentY ||
        drone.Status != request.Status ||
        drone.IsActive != request.IsActive;

    private static void Apply(Drone drone, UpdateDroneRequest request)
    {
        drone.Code = request.Code.Trim();
        drone.Name = request.Name.Trim();
        drone.MaxPackageWeightKg = request.MaxPackageWeightKg;
        drone.MaxRangeKm = request.MaxRangeKm;
        drone.BatteryLevelPercent = request.BatteryLevelPercent;
        drone.AverageSpeedKmPerHour = request.AverageSpeedKmPerHour;
        drone.BatteryConsumptionPercentagePerKm = request.BatteryConsumptionPercentagePerKm;
        drone.CurrentX = request.CurrentX;
        drone.CurrentY = request.CurrentY;
        drone.Status = request.IsActive ? request.Status : DroneStatus.Unavailable;
        drone.Notes = NormalizeNotes(request.Notes);
        drone.IsActive = request.IsActive;
    }

    private DroneResponse MapResponse(Drone drone, decimal globalSafetyMargin, bool hasExecutingTrip, bool hasPlannedTrips, DateTime utcNow)
    {
        var runtime = _chargingService.GetCurrentState(drone, utcNow);
        var status = drone.IsActive ? runtime.Status : DroneStatus.Unavailable;
        return new DroneResponse(
            drone.Id,
            drone.Code,
            drone.Name,
            drone.MaxPackageWeightKg,
            drone.MaxRangeKm,
            runtime.BatteryLevelPercent,
            globalSafetyMargin,
            drone.AverageSpeedKmPerHour,
            drone.BatteryConsumptionPercentagePerKm,
            drone.CurrentX,
            drone.CurrentY,
            hasExecutingTrip ? ResolveExecutingStatus(drone.Id, utcNow, status) : status,
            drone.Notes,
            drone.IsActive,
            hasExecutingTrip,
            hasPlannedTrips,
            runtime.ChargingStartedAtUtc,
            runtime.ChargingCompletedAtUtc,
            runtime.ChargingProgressPercentage,
            drone.CreatedAt,
            drone.UpdatedAt);
    }

    private DroneStatus ResolveExecutingStatus(int droneId, DateTime utcNow, DroneStatus fallback)
    {
        var trip = _dbContext.Trips
            .AsEnumerable()
            .FirstOrDefault(item => item.DroneId == droneId && item.CompletedAtUtc > utcNow && item.LoadingStartedAtUtc <= utcNow);
        if (trip is not null)
        {
            return _tripStateService.GetCurrentState(trip, utcNow).DroneStatus;
        }

        var delivery = _dbContext.Deliveries
            .AsEnumerable()
            .FirstOrDefault(item => item.DroneId == droneId && item.CompletedAtUtc > utcNow);
        return delivery is null ? fallback : _deliveryStateService.GetCurrentState(delivery, utcNow).DroneStatus;
    }

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
