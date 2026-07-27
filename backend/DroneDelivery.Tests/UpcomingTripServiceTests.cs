using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using DroneDelivery.Api.Services;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Tests;

public sealed class UpcomingTripServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsPlannedTripsThatHaveNotStarted()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = CreateDrone("DRN", capacity: 10, battery: 100);
        var order = CreateOrder("Order", weight: 2);
        dbContext.AddRange(drone, order);
        await dbContext.SaveChangesAsync();
        dbContext.Trips.Add(CreateTrip(drone.Id, order.Id, loadingOffsetMinutes: 10));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var response = await service.GetAsync(CancellationToken.None);

        var trip = Assert.Single(response.UpcomingTrips);
        Assert.Equal("DRN", trip.DroneCode);
        Assert.Equal("Aguardando inicio", trip.WaitingReason);
        Assert.Empty(response.UnplannedOrders);
    }

    [Fact]
    public async Task GetAsync_DoesNotDuplicateAlreadyStartedTrips()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = CreateDrone("DRN", capacity: 10, battery: 100);
        var order = CreateOrder("Order", weight: 2);
        dbContext.AddRange(drone, order);
        await dbContext.SaveChangesAsync();
        dbContext.Trips.Add(CreateTrip(drone.Id, order.Id, loadingOffsetMinutes: -1));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var response = await service.GetAsync(CancellationToken.None);

        Assert.Empty(response.UpcomingTrips);
    }

    [Fact]
    public async Task GetAsync_ReturnsUnplannedOrderWhenNoDroneHasCapacity()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Drones.Add(CreateDrone("DRN", capacity: 1, battery: 100));
        dbContext.Orders.Add(CreateOrder("Heavy", weight: 5));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var response = await service.GetAsync(CancellationToken.None);

        var order = Assert.Single(response.UnplannedOrders);
        Assert.Equal("WAITING_FOR_CAPACITY", order.WaitingCode);
        Assert.Empty(response.UpcomingTrips);
    }

    private static UpcomingTripService CreateService(DroneDelivery.Api.Data.DroneDeliveryDbContext dbContext)
    {
        var distance = new DistanceService();
        return new UpcomingTripService(
            dbContext,
            new RoutePlanningService(dbContext, distance),
            new ChargingService(Options.Create(new SimulationOptions { ChargingPercentagePointsPerSecond = 2 })),
            new DroneSettingsService(dbContext),
            new FakeClock(),
            Options.Create(new DroneDeliveryOptions { BaseX = 0, BaseY = 0, RequireReturnToBase = true }));
    }

    private static Drone CreateDrone(string code, decimal capacity, decimal battery) =>
        new()
        {
            Code = code,
            Name = code,
            MaxPackageWeightKg = capacity,
            MaxRangeKm = 100,
            BatteryLevelPercent = battery,
            AverageSpeedKmPerHour = 60,
            BatteryConsumptionPercentagePerKm = 1.5m,
            IsActive = true,
            Status = DroneStatus.Idle
        };

    private static DeliveryOrder CreateOrder(string name, decimal weight) =>
        new()
        {
            CustomerName = name,
            DestinationX = 1,
            DestinationY = 0,
            PackageWeightKg = weight,
            Priority = OrderPriority.High,
            Status = OrderStatus.Pending,
            QueueStatus = OrderQueueStatus.Queued,
            QueuedAtUtc = new DateTime(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc)
        };

    private static Trip CreateTrip(int droneId, int orderId, int loadingOffsetMinutes)
    {
        var now = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        return new Trip
        {
            DroneId = droneId,
            Status = TripStatus.Planned,
            PlannedAtUtc = now.AddMinutes(-5),
            LoadingStartedAtUtc = now.AddMinutes(loadingOffsetMinutes),
            FlyingStartedAtUtc = now.AddMinutes(loadingOffsetMinutes + 1),
            DeliveringStartedAtUtc = now.AddMinutes(loadingOffsetMinutes + 2),
            ReturningStartedAtUtc = now.AddMinutes(loadingOffsetMinutes + 3),
            CompletedAtUtc = now.AddMinutes(loadingOffsetMinutes + 4),
            TotalWeightKg = 2,
            EstimatedDistanceKm = 2,
            EstimatedBatteryConsumptionPercentagePoints = 3,
            BatterySafetyMarginPercentagePoints = 5,
            MinimumRequiredBatteryPercentage = 8,
            BatteryAtDeparturePercentage = 100,
            ExpectedBatteryAtReturnPercentage = 97,
            TripOrders = { new TripOrder { OrderId = orderId, DeliverySequence = 1, EstimatedArrivalAtUtc = now.AddMinutes(loadingOffsetMinutes + 2), DeliveryStartedAtUtc = now.AddMinutes(loadingOffsetMinutes + 2), DeliveryCompletedAtUtc = now.AddMinutes(loadingOffsetMinutes + 3) } }
        };
    }
}
