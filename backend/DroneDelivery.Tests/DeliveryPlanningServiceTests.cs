using DroneDelivery.Api.Data;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using DroneDelivery.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Tests;

public sealed class DeliveryPlanningServiceTests
{
    [Fact]
    public async Task PlanAsync_GroupsCompatibleOrdersIntoOneTrip()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN", capacity: 10, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("High", OrderPriority.High, weight: 3, x: 1, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Low", OrderPriority.Low, weight: 2, x: 2, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var plan = await service.PlanAsync(CancellationToken.None);

        var trip = Assert.Single(plan.Trips);
        Assert.Equal(2, trip.Orders.Count);
        Assert.Equal("High", trip.Orders[0].CustomerName);
        Assert.Equal("Low", trip.Orders[1].CustomerName);
        Assert.Equal(2, plan.OrdersAllocated);
    }

    [Fact]
    public async Task PlanAsync_QueuesPendingNotQueuedOrders()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN", capacity: 10, range: 100, battery: 100, margin: 5));
        dbContext.Orders.Add(new DeliveryOrder
        {
            CustomerName = "Created order",
            DestinationX = 1,
            DestinationY = 0,
            PackageWeightKg = 1,
            Priority = OrderPriority.High,
            Status = OrderStatus.Pending,
            QueueStatus = OrderQueueStatus.NotQueued
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var plan = await service.PlanAsync(CancellationToken.None);

        var trip = Assert.Single(plan.Trips);
        Assert.Equal("Created order", Assert.Single(trip.Orders).CustomerName);
        Assert.Equal(OrderQueueStatus.Planned, dbContext.Orders.Single().QueueStatus);
    }

    [Fact]
    public async Task PlanAsync_SplitsTripsWhenCapacityIsExceeded()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.AddRange(
            CreateDrone("A", capacity: 5, range: 100, battery: 100, margin: 5),
            CreateDrone("B", capacity: 5, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("One", OrderPriority.High, weight: 4, x: 1, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Two", OrderPriority.High, weight: 4, x: 2, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var plan = await service.PlanAsync(CancellationToken.None);

        Assert.Equal(2, plan.TripsCreated);
        Assert.All(plan.Trips, trip => Assert.True(trip.TotalWeightKg <= trip.MaximumWeightKg));
    }

    [Fact]
    public async Task PlanAsync_IgnoresCancelledDeliveredAndActiveTripOrders()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var busyDrone = CreateDrone("Busy", capacity: 10, range: 100, battery: 100, margin: 5);
        dbContext.Drones.AddRange(busyDrone, CreateDrone("Available", capacity: 10, range: 100, battery: 100, margin: 5));
        var cancelledOrder = CreateQueuedOrder("Cancelled", OrderPriority.High, weight: 2, x: 2, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc));
        cancelledOrder.Status = OrderStatus.Rejected;
        cancelledOrder.QueueStatus = OrderQueueStatus.Cancelled;
        var deliveredOrder = CreateQueuedOrder("Delivered", OrderPriority.High, weight: 2, x: 3, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 2, 0, DateTimeKind.Utc));
        deliveredOrder.Status = OrderStatus.Delivered;
        dbContext.Orders.AddRange(
            CreateQueuedOrder("Eligible", OrderPriority.High, weight: 2, x: 1, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            cancelledOrder,
            deliveredOrder);
        await dbContext.SaveChangesAsync();
        var activeOrder = CreateQueuedOrder("Active", OrderPriority.High, weight: 2, x: 4, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 3, 0, DateTimeKind.Utc));
        activeOrder.Status = OrderStatus.Allocated;
        activeOrder.QueueStatus = OrderQueueStatus.Planned;
        dbContext.Orders.Add(activeOrder);
        await dbContext.SaveChangesAsync();
        dbContext.Trips.Add(CreateTrip(busyDrone.Id, activeOrder.Id, new DateTime(2026, 7, 25, 13, 0, 0, DateTimeKind.Utc), started: true));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, clock: new FakeClock { UtcNow = new DateTime(2026, 7, 25, 13, 0, 0, DateTimeKind.Utc) });

        var plan = await service.PlanAsync(CancellationToken.None);

        var trip = Assert.Single(plan.Trips);
        Assert.Equal("Eligible", Assert.Single(trip.Orders).CustomerName);
    }

    [Fact]
    public async Task PlanAsync_OnlyUsesIdleActiveDrones()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var inactive = CreateDrone("Inactive", capacity: 10, range: 100, battery: 100, margin: 5);
        inactive.IsActive = false;
        var charging = CreateDrone("Charging", capacity: 10, range: 100, battery: 100, margin: 5);
        charging.Status = DroneStatus.Charging;
        charging.ChargingStartedAtUtc = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        charging.BatteryAtChargingStartPercentage = 0;
        charging.ChargingCompletedAtUtc = new DateTime(2026, 7, 25, 12, 0, 10, DateTimeKind.Utc);
        dbContext.Drones.AddRange(inactive, charging, CreateDrone("Idle", capacity: 10, range: 100, battery: 100, margin: 5));
        dbContext.Orders.Add(CreateQueuedOrder("Order", OrderPriority.High, weight: 1, x: 10, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, clock: new FakeClock { UtcNow = new DateTime(2026, 7, 25, 12, 0, 5, DateTimeKind.Utc) });

        var plan = await service.PlanAsync(CancellationToken.None);

        Assert.Equal("Idle", Assert.Single(plan.Trips).DroneCode);
    }

    [Fact]
    public async Task PlanAsync_RejectsDroneWithoutEnoughRange()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("ShortRange", capacity: 10, range: 9, battery: 100, margin: 5));
        dbContext.Orders.Add(CreateQueuedOrder("Order", OrderPriority.High, weight: 1, x: 5, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, batteryConsumptionPerKm: 0.1m);

        var plan = await service.PlanAsync(CancellationToken.None);

        Assert.Empty(plan.Trips);
        Assert.Equal("NO_VALID_DRONE_AVAILABLE", Assert.Single(plan.UnallocatedOrders).Reason);
    }

    [Fact]
    public async Task PlanAsync_PrefersCandidateWithMoreOrdersToReduceTrips()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.AddRange(
            CreateDrone("Small", capacity: 5, range: 100, battery: 100, margin: 5),
            CreateDrone("Large", capacity: 9, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("One", OrderPriority.High, weight: 3, x: 1, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Two", OrderPriority.High, weight: 3, x: 2, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Three", OrderPriority.High, weight: 3, x: 3, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 2, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var plan = await service.PlanAsync(CancellationToken.None);

        var trip = Assert.Single(plan.Trips);
        Assert.Equal("Large", trip.DroneCode);
        Assert.Equal(3, trip.Orders.Count);
    }

    [Fact]
    public async Task PlanAsync_ChoosesBestWholePlanWhenTripCountTies()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.AddRange(
            CreateDrone("Small", capacity: 4, range: 100, battery: 100, margin: 5),
            CreateDrone("Large", capacity: 9, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("High", OrderPriority.High, weight: 5, x: 10, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Near", OrderPriority.Medium, weight: 4, x: 11, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Far", OrderPriority.Medium, weight: 4, x: 0, y: 10, queuedAt: new DateTime(2026, 7, 25, 12, 2, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, batteryConsumptionPerKm: 0.1m);

        var plan = await service.PlanAsync(CancellationToken.None);

        Assert.Equal(2, plan.TripsCreated);
        var largeTrip = Assert.Single(plan.Trips, trip => trip.DroneCode == "Large");
        Assert.Collection(
            largeTrip.Orders,
            order => Assert.Equal("High", order.CustomerName),
            order => Assert.Equal("Near", order.CustomerName));
        Assert.True(plan.Trips.Sum(trip => trip.EstimatedDistanceKm) < 45m);
    }

    [Fact]
    public async Task PlanAsync_PriorityOverridesNearestNeighborDistance()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN", capacity: 10, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("LowNear", OrderPriority.Low, weight: 1, x: 1, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("HighFar", OrderPriority.High, weight: 1, x: 8, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("MediumMiddle", OrderPriority.Medium, weight: 1, x: 4, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 2, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var plan = await service.PlanAsync(CancellationToken.None);

        Assert.Collection(
            Assert.Single(plan.Trips).Orders,
            order => Assert.Equal("HighFar", order.CustomerName),
            order => Assert.Equal("MediumMiddle", order.CustomerName),
            order => Assert.Equal("LowNear", order.CustomerName));
    }

    [Fact]
    public async Task PlanAsync_UsesNearestNeighborAndDeterministicTieBreakerWithinSamePriority()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN", capacity: 10, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("Far", OrderPriority.High, weight: 1, x: 8, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Near", OrderPriority.High, weight: 1, x: 2, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("TieOld", OrderPriority.High, weight: 1, x: 2, y: 2, queuedAt: new DateTime(2026, 7, 25, 12, 2, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("TieNew", OrderPriority.High, weight: 1, x: 2, y: -2, queuedAt: new DateTime(2026, 7, 25, 12, 3, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var firstPlan = await service.PlanAsync(CancellationToken.None);
        var firstOrder = Assert.Single(firstPlan.Trips).Orders.Select(order => order.CustomerName).ToList();
        var secondPlan = await service.PlanAsync(CancellationToken.None);
        var secondOrder = Assert.Single(secondPlan.Trips).Orders.Select(order => order.CustomerName).ToList();

        Assert.Equal(new[] { "Near", "TieOld", "TieNew", "Far" }, firstOrder);
        Assert.Equal(firstOrder, secondOrder);
    }

    [Fact]
    public async Task PlanAsync_WhenTripCountTies_ChoosesLowerTotalDistance()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.AddRange(
            CreateDrone("Small", capacity: 3, range: 100, battery: 100, margin: 5),
            CreateDrone("Large", capacity: 10, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("High", OrderPriority.High, weight: 6, x: 9, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("NearHigh", OrderPriority.High, weight: 3, x: 10, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("FarLow", OrderPriority.Low, weight: 3, x: 0, y: 9, queuedAt: new DateTime(2026, 7, 25, 12, 2, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, batteryConsumptionPerKm: 0.1m);

        var plan = await service.PlanAsync(CancellationToken.None);

        Assert.Equal(2, plan.TripsCreated);
        var largeTrip = Assert.Single(plan.Trips, trip => trip.DroneCode == "Large");
        Assert.Collection(
            largeTrip.Orders,
            order => Assert.Equal("High", order.CustomerName),
            order => Assert.Equal("NearHigh", order.CustomerName));
    }

    [Fact]
    public async Task PlanAsync_AddsBatterySafetyMarginAsPercentagePoints()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN", capacity: 10, range: 100, battery: 45, margin: 5));
        dbContext.Orders.Add(CreateQueuedOrder("Order", OrderPriority.High, weight: 1, x: 20, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, batteryConsumptionPerKm: 1m, batterySafetyMarginPercentagePoints: 5m);

        var plan = await service.PlanAsync(CancellationToken.None);

        var trip = Assert.Single(plan.Trips);
        Assert.Equal(40m, trip.EstimatedBatteryConsumptionPercentagePoints);
        Assert.Equal(5m, trip.BatterySafetyMarginPercentagePoints);
        Assert.Equal(45m, trip.MinimumRequiredBatteryPercentage);
    }

    [Fact]
    public async Task PlanAsync_RejectsBatteryBelowMinimumRequired()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN", capacity: 10, range: 100, battery: 44, margin: 5));
        dbContext.Orders.Add(CreateQueuedOrder("Order", OrderPriority.High, weight: 1, x: 20, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, batteryConsumptionPerKm: 1m, batterySafetyMarginPercentagePoints: 5m);

        var plan = await service.PlanAsync(CancellationToken.None);

        Assert.Empty(plan.Trips);
        Assert.Equal("NO_VALID_DRONE_AVAILABLE", Assert.Single(plan.UnallocatedOrders).Reason);
    }

    [Fact]
    public async Task PlanAsync_DoesNotDuplicateOrdersAcrossTrips()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.AddRange(
            CreateDrone("A", capacity: 5, range: 100, battery: 100, margin: 5),
            CreateDrone("B", capacity: 5, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("One", OrderPriority.High, weight: 3, x: 1, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Two", OrderPriority.High, weight: 3, x: 2, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Three", OrderPriority.Medium, weight: 2, x: 3, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 2, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var plan = await service.PlanAsync(CancellationToken.None);

        var allocatedNames = plan.Trips.SelectMany(trip => trip.Orders).Select(order => order.CustomerName).ToList();
        Assert.Equal(allocatedNames.Count, allocatedNames.Distinct().Count());
        Assert.DoesNotContain(dbContext.Orders, order => order.Status == OrderStatus.Pending && order.QueueStatus == OrderQueueStatus.Planned);
    }

    [Fact]
    public async Task PlanAsync_ReplansMutableTripsWithoutDroppingQueuedOrders()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN", capacity: 10, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("One", OrderPriority.High, weight: 3, x: 1, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Two", OrderPriority.High, weight: 3, x: 2, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var clock = new FakeClock { UtcNow = new DateTime(2026, 7, 25, 13, 0, 0, DateTimeKind.Utc) };
        var service = CreateService(dbContext, clock: clock);

        var firstPlan = await service.PlanAsync(CancellationToken.None);
        var secondPlan = await service.PlanAsync(CancellationToken.None);

        Assert.Equal(2, firstPlan.OrdersAllocated);
        Assert.Equal(2, secondPlan.OrdersAllocated);
        Assert.Equal(2, Assert.Single(secondPlan.Trips).Orders.Count);
        Assert.All(dbContext.Orders, order => Assert.Equal(OrderStatus.Allocated, order.Status));
    }

    [Fact]
    public async Task ProcessQueueAsync_PlansRemainingQueuedOrdersAfterDroneReturns()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN", capacity: 5, range: 100, battery: 100, margin: 5));
        dbContext.Orders.AddRange(
            CreateQueuedOrder("First", OrderPriority.High, weight: 5, x: 1, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)),
            CreateQueuedOrder("Second", OrderPriority.High, weight: 5, x: 2, y: 0, queuedAt: new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var clock = new FakeClock { UtcNow = new DateTime(2026, 7, 25, 13, 0, 0, DateTimeKind.Utc) };
        var service = CreateService(dbContext, batteryConsumptionPerKm: 1m, clock: clock);

        var firstPlan = await service.PlanAsync(CancellationToken.None);
        clock.UtcNow = dbContext.Trips.Single().CompletedAtUtc;
        await service.GetTripsAsync(CancellationToken.None);

        var secondPlan = await service.ProcessQueueAsync(CancellationToken.None);

        Assert.Equal(1, firstPlan.OrdersAllocated);
        Assert.Equal(1, secondPlan.OrdersAllocated);
        Assert.Equal(2, dbContext.Trips.Count());
        Assert.Empty(dbContext.Orders.Where(order => order.QueueStatus == OrderQueueStatus.Queued));
        Assert.Equal(98m, dbContext.Drones.Single().BatteryLevelPercent);
        Assert.Null(dbContext.Drones.Single().ChargingCompletedAtUtc);
    }

    [Fact]
    public async Task ProcessQueueAsync_StopsChargingWhenCurrentBatteryCanServeQueuedOrder()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var now = new DateTime(2026, 7, 25, 13, 0, 0, DateTimeKind.Utc);
        var drone = CreateDrone("DRN", capacity: 5, range: 100, battery: 5, margin: 5);
        drone.Status = DroneStatus.Charging;
        drone.ChargingStartedAtUtc = now.AddSeconds(-5);
        drone.BatteryAtChargingStartPercentage = 5;
        drone.ChargingCompletedAtUtc = now.AddSeconds(45);
        dbContext.Drones.Add(drone);
        dbContext.Orders.Add(CreateQueuedOrder("Queued", OrderPriority.High, weight: 2, x: 2, y: 0, queuedAt: now.AddMinutes(-1)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, batteryConsumptionPerKm: 1m, clock: new FakeClock { UtcNow = now });

        var plan = await service.ProcessQueueAsync(CancellationToken.None);

        var trip = Assert.Single(plan.Trips);
        Assert.Equal(15m, trip.BatteryAtDeparturePercentage);
        Assert.Equal(15m, dbContext.Drones.Single().BatteryLevelPercent);
        Assert.Equal(DroneStatus.Idle, dbContext.Drones.Single().Status);
        Assert.Null(dbContext.Drones.Single().ChargingCompletedAtUtc);
        Assert.Equal(OrderQueueStatus.Planned, dbContext.Orders.Single().QueueStatus);
    }

    [Fact]
    public void ChargingService_CalculatesBatteryFromPersistedTimestamps()
    {
        var service = new ChargingService(Options.Create(new SimulationOptions { ChargingPercentagePointsPerSecond = 2 }));
        var startedAt = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var drone = CreateDrone("DRN", capacity: 10, range: 100, battery: 60, margin: 5);
        service.StartChargingIfNeeded(drone, 60, startedAt);

        var middle = service.GetCurrentState(drone, startedAt.AddSeconds(10));
        var completed = service.GetCurrentState(drone, drone.ChargingCompletedAtUtc!.Value);

        Assert.Equal(80m, middle.BatteryLevelPercent);
        Assert.Equal(DroneStatus.Charging, middle.Status);
        Assert.Equal(100m, completed.BatteryLevelPercent);
        Assert.Equal(DroneStatus.Idle, completed.Status);
    }

    [Fact]
    public void ChargingService_StartChargingUsesConfiguredRate()
    {
        var service = new ChargingService(Options.Create(new SimulationOptions { ChargingPercentagePointsPerSecond = 1m }));
        var startedAt = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var drone = CreateDrone("DRN", capacity: 10, range: 100, battery: 98, margin: 5);
        drone.ChargingRatePercentagePointsPerSecond = 2;

        service.StartChargingIfNeeded(drone, 98, startedAt);

        Assert.Equal(1m, drone.ChargingRatePercentagePointsPerSecond);
        Assert.Equal(startedAt.AddSeconds(2), drone.ChargingCompletedAtUtc);
    }

    private static DeliveryPlanningService CreateService(
        DroneDeliveryDbContext dbContext,
        decimal batteryConsumptionPerKm = 1.5m,
        decimal batterySafetyMarginPercentagePoints = 5m,
        FakeClock? clock = null)
    {
        foreach (var drone in dbContext.Drones.Local)
        {
            drone.BatteryConsumptionPercentagePerKm = batteryConsumptionPerKm;
        }

        dbContext.DroneSettings.RemoveRange(dbContext.DroneSettings);
        dbContext.DroneSettings.Add(new DroneSettings
        {
            Id = 1,
            BatterySafetyMarginPercentagePoints = batterySafetyMarginPercentagePoints
        });
        dbContext.SaveChanges();

        return new DeliveryPlanningService(
            dbContext,
            new DistanceService(),
            new RoutePlanningService(dbContext, new DistanceService()),
            new TripStateService(),
            new ChargingService(Options.Create(new SimulationOptions
            {
                LoadingDurationSeconds = 3,
                DeliveryDurationSeconds = 3,
                SecondsPerKilometer = 2,
                ChargingPercentagePointsPerSecond = 2
            })),
            new DroneOrderCapabilityService(
                dbContext,
                new RoutePlanningService(dbContext, new DistanceService()),
                new DroneSettingsService(dbContext),
                Options.Create(new DroneDeliveryOptions { BaseX = 0, BaseY = 0, RequireReturnToBase = true })),
            clock ?? new FakeClock(),
            new DroneSettingsService(dbContext),
            Options.Create(new DroneDeliveryOptions
            {
                BaseX = 0,
                BaseY = 0,
                BatteryConsumptionPerKm = batteryConsumptionPerKm,
                BatterySafetyMarginPercentagePoints = batterySafetyMarginPercentagePoints,
                DroneSpeedKmPerHour = 60,
                RequireReturnToBase = true
            }),
            Options.Create(new SimulationOptions
            {
                LoadingDurationSeconds = 3,
                DeliveryDurationSeconds = 3,
                SecondsPerKilometer = 2,
                ChargingPercentagePointsPerSecond = 2
            }),
            NullLogger<DeliveryPlanningService>.Instance);
    }

    private static Drone CreateDrone(string code, decimal capacity, decimal range, decimal battery, decimal margin) =>
        new()
        {
            Code = code,
            MaxPackageWeightKg = capacity,
            MaxRangeKm = range,
            BatteryLevelPercent = battery,
            BatterySafetyMarginPercentagePoints = margin,
            ChargingRatePercentagePointsPerSecond = 2,
            Status = DroneStatus.Idle
        };

    private static DeliveryOrder CreateQueuedOrder(string name, OrderPriority priority, decimal weight, decimal x, decimal y, DateTime queuedAt) =>
        new()
        {
            CustomerName = name,
            DestinationX = x,
            DestinationY = y,
            PackageWeightKg = weight,
            Priority = priority,
            Status = OrderStatus.Pending,
            QueueStatus = OrderQueueStatus.Queued,
            QueuedAtUtc = queuedAt
        };

    private static Trip CreateTrip(int droneId, int orderId, DateTime utcNow, bool started) =>
        new()
        {
            DroneId = droneId,
            Status = started ? TripStatus.Loading : TripStatus.Planned,
            PlannedAtUtc = utcNow.AddMinutes(-10),
            LoadingStartedAtUtc = started ? utcNow.AddMinutes(-1) : utcNow.AddMinutes(5),
            FlyingStartedAtUtc = utcNow.AddMinutes(1),
            DeliveringStartedAtUtc = utcNow.AddMinutes(2),
            ReturningStartedAtUtc = utcNow.AddMinutes(3),
            CompletedAtUtc = utcNow.AddMinutes(4),
            TotalWeightKg = 2,
            EstimatedDistanceKm = 2,
            EstimatedBatteryConsumptionPercentagePoints = 2,
            BatterySafetyMarginPercentagePoints = 5,
            MinimumRequiredBatteryPercentage = 7,
            BatteryAtDeparturePercentage = 100,
            ExpectedBatteryAtReturnPercentage = 98,
            TripOrders =
            {
                new TripOrder
                {
                    OrderId = orderId,
                    DeliverySequence = 1,
                    EstimatedArrivalAtUtc = utcNow.AddMinutes(2),
                    DeliveryStartedAtUtc = utcNow.AddMinutes(2),
                    DeliveryCompletedAtUtc = utcNow.AddMinutes(3)
                }
            }
        };
}
