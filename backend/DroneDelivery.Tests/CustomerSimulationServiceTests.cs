using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using DroneDelivery.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Tests;

public sealed class CustomerSimulationServiceTests
{
    [Fact]
    public async Task GetTrackingAsync_ReturnsPendingMessageWhenOrderHasNoDrone()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var order = CreateOrder();
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var tracking = await service.GetTrackingAsync(order.Id, CancellationToken.None);

        Assert.Equal("Pedido recebido", tracking.FriendlyStatus);
        Assert.Null(tracking.DroneCode);
        Assert.Equal(0, tracking.ProgressPercentage);
    }

    [Fact]
    public async Task GetTrackingAsync_InterpolatesPositionDuringTrip()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = new Drone { Code = "DRN", Name = "Drone", MaxPackageWeightKg = 10, MaxRangeKm = 100, BatteryLevelPercent = 100, AverageSpeedKmPerHour = 60, BatteryConsumptionPercentagePerKm = 1.5m, IsActive = true };
        var order = CreateOrder();
        dbContext.AddRange(drone, order);
        await dbContext.SaveChangesAsync();
        dbContext.Trips.Add(new Trip
        {
            DroneId = drone.Id,
            Status = TripStatus.Flying,
            PlannedAtUtc = new DateTime(2026, 7, 25, 11, 59, 0, DateTimeKind.Utc),
            LoadingStartedAtUtc = new DateTime(2026, 7, 25, 11, 59, 0, DateTimeKind.Utc),
            FlyingStartedAtUtc = new DateTime(2026, 7, 25, 11, 59, 30, DateTimeKind.Utc),
            DeliveringStartedAtUtc = new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc),
            ReturningStartedAtUtc = new DateTime(2026, 7, 25, 12, 2, 0, DateTimeKind.Utc),
            CompletedAtUtc = new DateTime(2026, 7, 25, 12, 2, 0, DateTimeKind.Utc),
            TotalWeightKg = 1,
            EstimatedDistanceKm = 20,
            EstimatedBatteryConsumptionPercentagePoints = 10,
            BatterySafetyMarginPercentagePoints = 5,
            MinimumRequiredBatteryPercentage = 15,
            BatteryAtDeparturePercentage = 100,
            ExpectedBatteryAtReturnPercentage = 90,
            TripOrders = { new TripOrder { OrderId = order.Id, DeliverySequence = 1, EstimatedArrivalAtUtc = new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc), DeliveryStartedAtUtc = new DateTime(2026, 7, 25, 12, 1, 0, DateTimeKind.Utc), DeliveryCompletedAtUtc = new DateTime(2026, 7, 25, 12, 1, 10, DateTimeKind.Utc) } }
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var tracking = await service.GetTrackingAsync(order.Id, CancellationToken.None);

        Assert.Equal("Seu pacote esta a caminho", tracking.FriendlyStatus);
        Assert.True(tracking.CurrentPosition.X > 0);
        Assert.True(tracking.RemainingDistance > 0);
    }

    [Fact]
    public async Task CreateOrderAsync_CreatesAndQueuesOrder()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);

        var created = await service.CreateOrderAsync(new CustomerOrderRequest("Cliente", "Caixa", 1, 1, 0, OrderPriority.High), CancellationToken.None);

        var order = dbContext.Orders.Single(item => item.Id == created.OrderId);
        Assert.Equal(OrderQueueStatus.Queued, order.QueueStatus);
    }

    private static CustomerSimulationService CreateService(DroneDelivery.Api.Data.DroneDeliveryDbContext dbContext)
    {
        var clock = new FakeClock();
        var distance = new DistanceService();
        var route = new RoutePlanningService(dbContext, distance);
        var tripState = new TripStateService();
        var deliveryState = new DeliveryStateService();
        var orderService = new OrderService(dbContext, distance, route, deliveryState, tripState, clock);
        var planning = new DeliveryPlanningService(
            dbContext,
            distance,
            route,
            tripState,
            new ChargingService(Options.Create(new SimulationOptions { ChargingPercentagePointsPerSecond = 2 })),
            new DroneOrderCapabilityService(
                dbContext,
                route,
                new DroneSettingsService(dbContext),
                Options.Create(new DroneDeliveryOptions { BaseX = 0, BaseY = 0, RequireReturnToBase = true })),
            clock,
            new DroneSettingsService(dbContext),
            Options.Create(new DroneDeliveryOptions { BaseX = 0, BaseY = 0, RequireReturnToBase = true }),
            Options.Create(new SimulationOptions { LoadingDurationSeconds = 3, DeliveryDurationSeconds = 3, SecondsPerKilometer = 2, ChargingPercentagePointsPerSecond = 2 }),
            NullLogger<DeliveryPlanningService>.Instance);
        return new CustomerSimulationService(dbContext, orderService, planning, tripState, deliveryState, distance, clock);
    }

    private static DeliveryOrder CreateOrder() =>
        new()
        {
            CustomerName = "Cliente",
            DestinationX = 10,
            DestinationY = 0,
            PackageWeightKg = 1,
            Priority = OrderPriority.High,
            Status = OrderStatus.Pending,
            QueueStatus = OrderQueueStatus.NotQueued
        };
}
