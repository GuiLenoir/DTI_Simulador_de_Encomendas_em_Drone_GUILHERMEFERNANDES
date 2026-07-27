using DroneDelivery.Api.Data;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using DroneDelivery.Api.Services;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Tests;

public sealed class DeliveryServiceTests
{
    [Fact]
    public async Task AllocateAsync_RejectsPackageHeavierThanEveryDrone()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("Small", 1, 100, 100, 0, 0));
        var order = CreateOrder(weight: 5, x: 1, y: 1);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<AllocationException>(() => service.AllocateAsync(order.Id, CancellationToken.None));

        Assert.Equal("NO_ELIGIBLE_DRONE", exception.Code);
    }

    [Fact]
    public async Task AllocateAsync_IgnoresUnavailableDrones()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("Unavailable", 10, 100, 100, 0, 0, DroneStatus.Flying));
        var order = CreateOrder(weight: 1, x: 1, y: 1);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<AllocationException>(() => service.AllocateAsync(order.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AllocateAsync_IgnoresDronesWithoutEnoughRange()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("Short", 10, 2, 100, 0, 0));
        var order = CreateOrder(weight: 1, x: 3, y: 4);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<AllocationException>(() => service.AllocateAsync(order.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AllocateAsync_IgnoresDronesWithoutEnoughBattery()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("LowBattery", 10, 100, 1, 0, 0));
        var order = CreateOrder(weight: 1, x: 3, y: 4);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<AllocationException>(() => service.AllocateAsync(order.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AllocateAsync_SelectsNearestEligibleDrone()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.AddRange(
            CreateDrone("Far", 10, 100, 100, 10, 0),
            CreateDrone("Near", 10, 100, 100, 1, 0));
        var order = CreateOrder(weight: 1, x: 2, y: 0);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var delivery = await service.AllocateAsync(order.Id, CancellationToken.None);

        Assert.Equal("Near", delivery.DroneCode);
    }

    [Fact]
    public async Task SimulateAsync_DecreasesBatteryAndReturnsDroneToIdle()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("Drone", 10, 100, 100, 0, 0));
        var order = CreateOrder(weight: 1, x: 3, y: 4);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var clock = new FakeClock();
        var service = CreateService(dbContext, clock);
        var allocated = await service.AllocateAsync(order.Id, CancellationToken.None);
        clock.UtcNow = dbContext.Deliveries.Single(delivery => delivery.Id == allocated.Id).CompletedAtUtc;

        var simulated = await service.SimulateAsync(allocated.Id, CancellationToken.None);
        var drone = dbContext.Drones.Single();

        Assert.Equal(DeliveryStatus.Delivered, simulated.Status);
        Assert.Equal(DroneStatus.Charging, drone.Status);
        Assert.Equal(85m, drone.BatteryLevelPercent);
        Assert.Equal(0m, drone.CurrentX);
        Assert.Equal(0m, drone.CurrentY);
    }

    [Fact]
    public async Task AllocateAsync_DoesNotReuseDroneWithActiveDelivery()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("Drone", 10, 100, 100, 0, 0));
        var firstOrder = CreateOrder(weight: 1, x: 3, y: 4);
        var secondOrder = CreateOrder(weight: 1, x: 1, y: 1);
        dbContext.Orders.AddRange(firstOrder, secondOrder);
        await dbContext.SaveChangesAsync();
        var clock = new FakeClock();
        var service = CreateService(dbContext, clock);

        await service.AllocateAsync(firstOrder.Id, CancellationToken.None);

        await Assert.ThrowsAsync<AllocationException>(() => service.AllocateAsync(secondOrder.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AllocateAsync_ReusesDroneAfterTimelineCompletion()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("Drone", 10, 100, 100, 0, 0));
        var firstOrder = CreateOrder(weight: 1, x: 1, y: 0);
        var secondOrder = CreateOrder(weight: 1, x: 1, y: 1);
        dbContext.Orders.AddRange(firstOrder, secondOrder);
        await dbContext.SaveChangesAsync();
        var clock = new FakeClock();
        var service = CreateService(dbContext, clock);

        var firstDelivery = await service.AllocateAsync(firstOrder.Id, CancellationToken.None);
        clock.UtcNow = dbContext.Deliveries.Single(delivery => delivery.Id == firstDelivery.Id).CompletedAtUtc;
        await service.GetAllAsync(CancellationToken.None);

        var secondDelivery = await service.AllocateAsync(secondOrder.Id, CancellationToken.None);

        Assert.Equal("Drone", secondDelivery.DroneCode);
        Assert.Null(dbContext.Drones.Single().ChargingCompletedAtUtc);
    }

    [Fact]
    public async Task GetAllAsync_CompletesElapsedDeliveries()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("Drone", 10, 100, 100, 0, 0));
        var order = CreateOrder(weight: 1, x: 3, y: 4);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var clock = new FakeClock();
        var service = CreateService(dbContext, clock);
        var allocated = await service.AllocateAsync(order.Id, CancellationToken.None);
        clock.UtcNow = dbContext.Deliveries.Single(delivery => delivery.Id == allocated.Id).CompletedAtUtc;

        var deliveries = await service.GetAllAsync(CancellationToken.None);

        var delivery = Assert.Single(deliveries);
        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
        Assert.Equal(OrderStatus.Delivered, dbContext.Orders.Single().Status);
        Assert.Equal(DroneStatus.Charging, dbContext.Drones.Single().Status);
    }

    private static DeliveryService CreateService(DroneDeliveryDbContext dbContext, FakeClock? clock = null) =>
        new(
            dbContext,
            new DistanceService(),
            new RoutePlanningService(dbContext, new DistanceService()),
            new DroneStateService(),
            new DeliveryStateService(),
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
                BatteryConsumptionPerKm = 1.5m,
                DroneSpeedKmPerHour = 60,
                RequireReturnToBase = true
            }),
            Options.Create(new SimulationOptions
            {
                LoadingDurationSeconds = 3,
                DeliveryDurationSeconds = 3,
                SecondsPerKilometer = 2
            }));

    private static Drone CreateDrone(
        string code,
        decimal capacity,
        decimal range,
        decimal battery,
        decimal x,
        decimal y,
        DroneStatus status = DroneStatus.Idle) =>
        new()
        {
            Code = code,
            MaxPackageWeightKg = capacity,
            MaxRangeKm = range,
            BatteryLevelPercent = battery,
            CurrentX = x,
            CurrentY = y,
            Status = status
        };

    private static DeliveryOrder CreateOrder(decimal weight, decimal x, decimal y) =>
        new()
        {
            CustomerName = "Customer",
            DestinationX = x,
            DestinationY = y,
            PackageWeightKg = weight,
            Priority = OrderPriority.High,
            Status = OrderStatus.Pending
        };
}
