using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using DroneDelivery.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Tests;

public sealed class DroneServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesDroneWithOperationalData()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);

        var drone = await service.CreateAsync(CreateRequest("DRN-900"), CancellationToken.None);

        Assert.Equal("DRN-900", drone.Code);
        Assert.Equal("Drone DRN-900", drone.Name);
        Assert.True(drone.IsActive);
        Assert.Equal(60m, drone.AverageSpeedKmPerHour);
        Assert.Equal(2.5m, drone.BatteryConsumptionPercentagePerKm);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateCode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN-001"));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(CreateRequest("DRN-001"), CancellationToken.None));

        Assert.Equal("DRONE_CODE_ALREADY_EXISTS", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidBatteryCapacityAndRange()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);

        Assert.Equal("INVALID_BATTERY_PERCENTAGE", (await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateRequest("A") with { BatteryLevelPercent = 101 }, CancellationToken.None))).Code);
        Assert.Equal("INVALID_DRONE_CAPACITY", (await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateRequest("B") with { MaxPackageWeightKg = 0 }, CancellationToken.None))).Code);
        Assert.Equal("INVALID_DRONE_RANGE", (await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateRequest("C") with { MaxRangeKm = 0 }, CancellationToken.None))).Code);
    }

    [Fact]
    public async Task UpdateAsync_AllowsNameAndNotesWhileExecutingButBlocksOperationalData()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = CreateDrone("DRN");
        dbContext.Drones.Add(drone);
        await dbContext.SaveChangesAsync();
        var now = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        dbContext.Trips.Add(new Trip
        {
            DroneId = drone.Id,
            Status = TripStatus.Loading,
            PlannedAtUtc = now.AddMinutes(-1),
            LoadingStartedAtUtc = now.AddMinutes(-1),
            FlyingStartedAtUtc = now.AddMinutes(1),
            DeliveringStartedAtUtc = now.AddMinutes(2),
            ReturningStartedAtUtc = now.AddMinutes(3),
            CompletedAtUtc = now.AddMinutes(4),
            TotalWeightKg = 1,
            EstimatedDistanceKm = 1,
            EstimatedBatteryConsumptionPercentagePoints = 1,
            BatterySafetyMarginPercentagePoints = 5,
            MinimumRequiredBatteryPercentage = 6,
            BatteryAtDeparturePercentage = 100,
            ExpectedBatteryAtReturnPercentage = 99
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var renamed = await service.UpdateAsync(drone.Id, ToRequest(drone) with { Name = "Novo nome", Notes = "ok" }, CancellationToken.None);
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(drone.Id, ToRequest(drone) with { MaxRangeKm = 200 }, CancellationToken.None));

        Assert.Equal("Novo nome", renamed.Name);
        Assert.Equal("DRONE_IS_EXECUTING_TRIP", exception.Code);
    }

    [Fact]
    public async Task DeactivateAsync_CancelsPlannedTripsAndPreservesCompletedTrips()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = CreateDrone("DRN");
        var queued = CreateOrder("Queued", OrderQueueStatus.Planned, OrderStatus.Allocated);
        var delivered = CreateOrder("Delivered", OrderQueueStatus.Completed, OrderStatus.Delivered);
        dbContext.AddRange(drone, queued, delivered);
        await dbContext.SaveChangesAsync();
        dbContext.Trips.Add(new Trip
        {
            DroneId = drone.Id,
            Status = TripStatus.Planned,
            PlannedAtUtc = DateTime.UtcNow,
            LoadingStartedAtUtc = DateTime.UtcNow.AddMinutes(5),
            FlyingStartedAtUtc = DateTime.UtcNow.AddMinutes(6),
            DeliveringStartedAtUtc = DateTime.UtcNow.AddMinutes(7),
            ReturningStartedAtUtc = DateTime.UtcNow.AddMinutes(8),
            CompletedAtUtc = DateTime.UtcNow.AddMinutes(9),
            TotalWeightKg = 1,
            EstimatedDistanceKm = 1,
            EstimatedBatteryConsumptionPercentagePoints = 1,
            BatterySafetyMarginPercentagePoints = 5,
            MinimumRequiredBatteryPercentage = 6,
            BatteryAtDeparturePercentage = 100,
            ExpectedBatteryAtReturnPercentage = 99,
            TripOrders = { new TripOrder { OrderId = queued.Id, DeliverySequence = 1, EstimatedArrivalAtUtc = DateTime.UtcNow.AddMinutes(7), DeliveryStartedAtUtc = DateTime.UtcNow.AddMinutes(7), DeliveryCompletedAtUtc = DateTime.UtcNow.AddMinutes(8) } }
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.DeactivateAsync(drone.Id, CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.Equal(OrderQueueStatus.Queued, dbContext.Orders.Single(order => order.Id == queued.Id).QueueStatus);
        Assert.Equal(OrderQueueStatus.Completed, dbContext.Orders.Single(order => order.Id == delivered.Id).QueueStatus);
    }

    [Fact]
    public async Task ActivateAsync_ReactivatesDrone()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = CreateDrone("DRN");
        drone.IsActive = false;
        drone.Status = DroneStatus.Unavailable;
        dbContext.Drones.Add(drone);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ActivateAsync(drone.Id, CancellationToken.None);

        Assert.True(result.IsActive);
        Assert.Equal(DroneStatus.Idle, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDroneFromDatabase()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = CreateDrone("DRN");
        dbContext.Drones.Add(drone);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await service.DeleteAsync(drone.Id, CancellationToken.None);

        Assert.Empty(dbContext.Drones);
    }

    [Fact]
    public async Task DeleteAsync_BlocksExecutingDrone()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var now = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var drone = CreateDrone("DRN");
        dbContext.Drones.Add(drone);
        await dbContext.SaveChangesAsync();
        dbContext.Trips.Add(new Trip
        {
            DroneId = drone.Id,
            Status = TripStatus.Loading,
            PlannedAtUtc = now.AddMinutes(-1),
            LoadingStartedAtUtc = now.AddMinutes(-1),
            FlyingStartedAtUtc = now.AddMinutes(1),
            DeliveringStartedAtUtc = now.AddMinutes(2),
            ReturningStartedAtUtc = now.AddMinutes(3),
            CompletedAtUtc = now.AddMinutes(4),
            TotalWeightKg = 1,
            EstimatedDistanceKm = 1,
            EstimatedBatteryConsumptionPercentagePoints = 1,
            BatterySafetyMarginPercentagePoints = 5,
            MinimumRequiredBatteryPercentage = 6,
            BatteryAtDeparturePercentage = 100,
            ExpectedBatteryAtReturnPercentage = 99
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(drone.Id, CancellationToken.None));

        Assert.Equal("DRONE_IS_EXECUTING_TRIP", exception.Code);
        Assert.Single(dbContext.Drones);
    }

    [Fact]
    public async Task GetAllAsync_CompletesElapsedIndividualDeliveriesAndUpdatesBattery()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var now = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var drone = CreateDrone("DRN");
        var order = CreateOrder("Order", OrderQueueStatus.Allocated, OrderStatus.InTransit);
        dbContext.AddRange(drone, order);
        await dbContext.SaveChangesAsync();
        dbContext.Deliveries.Add(new Delivery
        {
            DroneId = drone.Id,
            OrderId = order.Id,
            Status = DeliveryStatus.InTransit,
            StartX = 0,
            StartY = 0,
            DestinationX = 3,
            DestinationY = 4,
            EndX = 0,
            EndY = 0,
            EstimatedDistanceKm = 10,
            EstimatedBatteryConsumptionPercent = 25,
            EstimatedDurationMinutes = 10,
            AllocatedAt = now.AddMinutes(-1),
            CreatedAtUtc = now.AddMinutes(-1),
            LoadingStartedAtUtc = now.AddMinutes(-1),
            FlyingStartedAtUtc = now.AddSeconds(-50),
            DeliveringStartedAtUtc = now.AddSeconds(-40),
            ReturningStartedAtUtc = now.AddSeconds(-30),
            CompletedAtUtc = now
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetAllAsync(CancellationToken.None);

        var response = Assert.Single(result);
        Assert.Equal(75m, response.BatteryLevelPercent);
        Assert.Equal(75m, dbContext.Drones.Single().BatteryLevelPercent);
        Assert.Equal(OrderStatus.Delivered, dbContext.Orders.Single().Status);
    }

    [Fact]
    public async Task Planning_IgnoresInactiveDrones()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var inactive = CreateDrone("DRN");
        inactive.IsActive = false;
        dbContext.Drones.Add(inactive);
        dbContext.Orders.Add(CreateOrder("Order", OrderQueueStatus.Queued, OrderStatus.Pending));
        await dbContext.SaveChangesAsync();
        var planning = CreatePlanningService(dbContext);

        var plan = await planning.PlanAsync(CancellationToken.None);

        Assert.Empty(plan.Trips);
    }

    [Fact]
    public async Task SettingsService_ReadsAndUpdatesGlobalSafetyMargin()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new DroneSettingsService(dbContext);

        var initial = await service.GetAsync(CancellationToken.None);
        var updated = await service.UpdateAsync(new UpdateDroneSettingsRequest(12), CancellationToken.None);

        Assert.Equal(5m, initial.BatterySafetyMarginPercentagePoints);
        Assert.Equal(12m, updated.BatterySafetyMarginPercentagePoints);
    }

    private static DroneService CreateService(DroneDeliveryDbContext dbContext)
    {
        var settingsService = new DroneSettingsService(dbContext);
        return new DroneService(
            dbContext,
            new DeliveryStateService(),
            new TripStateService(),
            new ChargingService(Options.Create(new SimulationOptions { ChargingPercentagePointsPerSecond = 2 })),
            new FakeClock(),
            settingsService,
            CreatePlanningService(dbContext),
            CreateDeliveryService(dbContext));
    }

    private static DeliveryService CreateDeliveryService(DroneDeliveryDbContext dbContext) =>
        new(
            dbContext,
            new DistanceService(),
            new RoutePlanningService(dbContext, new DistanceService()),
            new DroneStateService(),
            new DeliveryStateService(),
            new TripStateService(),
            new ChargingService(Options.Create(new SimulationOptions { ChargingPercentagePointsPerSecond = 1 })),
            new DroneOrderCapabilityService(
                dbContext,
                new RoutePlanningService(dbContext, new DistanceService()),
                new DroneSettingsService(dbContext),
                Options.Create(new DroneDeliveryOptions { BaseX = 0, BaseY = 0, RequireReturnToBase = true })),
            new FakeClock(),
            new DroneSettingsService(dbContext),
            Options.Create(new DroneDeliveryOptions { BaseX = 0, BaseY = 0, RequireReturnToBase = true }),
            Options.Create(new SimulationOptions { LoadingDurationSeconds = 3, DeliveryDurationSeconds = 3, SecondsPerKilometer = 2, ChargingPercentagePointsPerSecond = 1 }));

    private static DeliveryPlanningService CreatePlanningService(DroneDeliveryDbContext dbContext) =>
        new(
            dbContext,
            new DistanceService(),
            new RoutePlanningService(dbContext, new DistanceService()),
            new TripStateService(),
            new ChargingService(Options.Create(new SimulationOptions { ChargingPercentagePointsPerSecond = 2 })),
            new DroneOrderCapabilityService(
                dbContext,
                new RoutePlanningService(dbContext, new DistanceService()),
                new DroneSettingsService(dbContext),
                Options.Create(new DroneDeliveryOptions { BaseX = 0, BaseY = 0, RequireReturnToBase = true })),
            new FakeClock(),
            new DroneSettingsService(dbContext),
            Options.Create(new DroneDeliveryOptions { BaseX = 0, BaseY = 0, RequireReturnToBase = true }),
            Options.Create(new SimulationOptions { LoadingDurationSeconds = 3, DeliveryDurationSeconds = 3, SecondsPerKilometer = 2, ChargingPercentagePointsPerSecond = 2 }),
            NullLogger<DeliveryPlanningService>.Instance);

    private static CreateDroneRequest CreateRequest(string code) =>
        new(code, $"Drone {code}", 5, 30, 100, 60, 2.5m, 0, 0, DroneStatus.Idle, null, true);

    private static UpdateDroneRequest ToRequest(Drone drone) =>
        new(drone.Code, drone.Name, drone.MaxPackageWeightKg, drone.MaxRangeKm, drone.BatteryLevelPercent,
            drone.AverageSpeedKmPerHour, drone.BatteryConsumptionPercentagePerKm, drone.CurrentX, drone.CurrentY,
            drone.Status, drone.Notes, drone.IsActive);

    private static Drone CreateDrone(string code) =>
        new()
        {
            Code = code,
            Name = $"Drone {code}",
            MaxPackageWeightKg = 5,
            MaxRangeKm = 30,
            BatteryLevelPercent = 100,
            AverageSpeedKmPerHour = 60,
            BatteryConsumptionPercentagePerKm = 1.5m,
            Status = DroneStatus.Idle,
            IsActive = true
        };

    private static DeliveryOrder CreateOrder(string name, OrderQueueStatus queueStatus, OrderStatus status) =>
        new()
        {
            CustomerName = name,
            DestinationX = 1,
            DestinationY = 0,
            PackageWeightKg = 1,
            Priority = OrderPriority.High,
            QueueStatus = queueStatus,
            Status = status,
            QueuedAtUtc = DateTime.UtcNow
        };
}
