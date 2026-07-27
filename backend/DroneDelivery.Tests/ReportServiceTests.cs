using DroneDelivery.Api.Models;
using DroneDelivery.Api.Services;

namespace DroneDelivery.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task GetAsync_CountsOnlyCompletedDeliveriesAndCalculatesAverageTime()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = new Drone { Code = "DRN", Name = "Drone", MaxPackageWeightKg = 10, MaxRangeKm = 100, BatteryLevelPercent = 100, AverageSpeedKmPerHour = 60, BatteryConsumptionPercentagePerKm = 1.5m, IsActive = true };
        dbContext.Drones.Add(drone);
        await dbContext.SaveChangesAsync();
        dbContext.Deliveries.AddRange(
            new Delivery { DroneId = drone.Id, Order = CreateOrder("A"), StartX = 0, StartY = 0, DestinationX = 1, DestinationY = 0, EndX = 0, EndY = 0, EstimatedDistanceKm = 2, EstimatedBatteryConsumptionPercent = 3, LoadingStartedAtUtc = new DateTime(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc), CompletedAtUtc = new DateTime(2026, 7, 25, 11, 1, 0, DateTimeKind.Utc), AllocatedAt = new DateTime(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc) },
            new Delivery { DroneId = drone.Id, Order = CreateOrder("B"), StartX = 0, StartY = 0, DestinationX = 2, DestinationY = 0, EndX = 0, EndY = 0, EstimatedDistanceKm = 4, EstimatedBatteryConsumptionPercent = 6, LoadingStartedAtUtc = new DateTime(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc), CompletedAtUtc = new DateTime(2026, 7, 25, 11, 3, 0, DateTimeKind.Utc), AllocatedAt = new DateTime(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc) },
            new Delivery { DroneId = drone.Id, Order = CreateOrder("C"), StartX = 0, StartY = 0, DestinationX = 20, DestinationY = 0, EndX = 0, EndY = 0, EstimatedDistanceKm = 40, EstimatedBatteryConsumptionPercent = 60, LoadingStartedAtUtc = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc), CompletedAtUtc = new DateTime(2026, 7, 25, 13, 0, 0, DateTimeKind.Utc), AllocatedAt = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc) });
        await dbContext.SaveChangesAsync();
        var service = new ReportService(dbContext, new DistanceService(), new FakeClock());

        var report = await service.GetAsync(null, new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc), null, null, CancellationToken.None);

        Assert.Equal(2, report.Summary.CompletedDeliveries);
        Assert.Equal(120, report.Summary.AverageDeliverySeconds);
        Assert.Equal("DRN", report.MostEfficientDrone!.DroneCode);
        Assert.Equal(2, report.Map.DisplayedDeliveries);
    }

    [Fact]
    public async Task GetAsync_ReturnsNoEfficiencyWhenThereIsNoData()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new ReportService(dbContext, new DistanceService(), new FakeClock());

        var report = await service.GetAsync(null, null, null, null, CancellationToken.None);

        Assert.Equal(0, report.Summary.CompletedDeliveries);
        Assert.Null(report.MostEfficientDrone);
    }

    [Fact]
    public async Task GetAsync_AppliesDroneAndPriorityFilters()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var firstDrone = CreateDrone("DRN-A");
        var secondDrone = CreateDrone("DRN-B");
        dbContext.Drones.AddRange(firstDrone, secondDrone);
        await dbContext.SaveChangesAsync();
        dbContext.Deliveries.AddRange(
            CreateDelivery(firstDrone.Id, CreateOrder("High", OrderPriority.High), completedAt: new DateTime(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc)),
            CreateDelivery(secondDrone.Id, CreateOrder("Low", OrderPriority.Low), completedAt: new DateTime(2026, 7, 25, 11, 5, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();
        var service = new ReportService(dbContext, new DistanceService(), new FakeClock());

        var report = await service.GetAsync(null, null, firstDrone.Id, OrderPriority.High, CancellationToken.None);

        Assert.Equal(1, report.Summary.CompletedDeliveries);
        Assert.Equal("DRN-A", report.MostEfficientDrone!.DroneCode);
        Assert.All(report.Map.Journeys, journey => Assert.Equal(firstDrone.Id, journey.DroneId));
    }

    [Fact]
    public async Task GetAsync_GroupsTripStopsInDeliverySequenceOrder()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = CreateDrone("DRN");
        var first = CreateOrder("First", OrderPriority.High);
        var second = CreateOrder("Second", OrderPriority.Medium);
        dbContext.AddRange(drone, first, second);
        await dbContext.SaveChangesAsync();
        dbContext.Trips.Add(new Trip
        {
            DroneId = drone.Id,
            Status = TripStatus.Completed,
            PlannedAtUtc = new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc),
            LoadingStartedAtUtc = new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc),
            FlyingStartedAtUtc = new DateTime(2026, 7, 25, 10, 0, 3, DateTimeKind.Utc),
            DeliveringStartedAtUtc = new DateTime(2026, 7, 25, 10, 0, 10, DateTimeKind.Utc),
            ReturningStartedAtUtc = new DateTime(2026, 7, 25, 10, 0, 20, DateTimeKind.Utc),
            CompletedAtUtc = new DateTime(2026, 7, 25, 10, 0, 30, DateTimeKind.Utc),
            TotalWeightKg = 3,
            EstimatedDistanceKm = 12,
            EstimatedBatteryConsumptionPercentagePoints = 18,
            BatterySafetyMarginPercentagePoints = 5,
            MinimumRequiredBatteryPercentage = 23,
            BatteryAtDeparturePercentage = 100,
            ExpectedBatteryAtReturnPercentage = 82,
            TripOrders =
            {
                new TripOrder { OrderId = second.Id, DeliverySequence = 2, EstimatedArrivalAtUtc = new DateTime(2026, 7, 25, 10, 0, 15, DateTimeKind.Utc), DeliveryStartedAtUtc = new DateTime(2026, 7, 25, 10, 0, 15, DateTimeKind.Utc), DeliveryCompletedAtUtc = new DateTime(2026, 7, 25, 10, 0, 18, DateTimeKind.Utc) },
                new TripOrder { OrderId = first.Id, DeliverySequence = 1, EstimatedArrivalAtUtc = new DateTime(2026, 7, 25, 10, 0, 10, DateTimeKind.Utc), DeliveryStartedAtUtc = new DateTime(2026, 7, 25, 10, 0, 10, DateTimeKind.Utc), DeliveryCompletedAtUtc = new DateTime(2026, 7, 25, 10, 0, 13, DateTimeKind.Utc) }
            }
        });
        await dbContext.SaveChangesAsync();
        var service = new ReportService(dbContext, new DistanceService(), new FakeClock { UtcNow = new DateTime(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc) });

        var report = await service.GetAsync(null, null, null, null, CancellationToken.None);

        var journey = Assert.Single(report.Map.Journeys);
        Assert.Equal(2, report.Summary.CompletedDeliveries);
        Assert.Collection(
            journey.Points,
            point => Assert.Equal("Base", point.Type),
            point => Assert.Equal(first.Id, point.OrderId),
            point => Assert.Equal(second.Id, point.OrderId));
    }

    private static Drone CreateDrone(string code) =>
        new()
        {
            Code = code,
            Name = code,
            MaxPackageWeightKg = 10,
            MaxRangeKm = 100,
            BatteryLevelPercent = 100,
            AverageSpeedKmPerHour = 60,
            BatteryConsumptionPercentagePerKm = 1.5m,
            IsActive = true
        };

    private static Delivery CreateDelivery(int droneId, DeliveryOrder order, DateTime completedAt) =>
        new()
        {
            DroneId = droneId,
            Order = order,
            StartX = 0,
            StartY = 0,
            DestinationX = order.DestinationX,
            DestinationY = order.DestinationY,
            EndX = 0,
            EndY = 0,
            EstimatedDistanceKm = 2,
            EstimatedBatteryConsumptionPercent = 3,
            LoadingStartedAtUtc = completedAt.AddMinutes(-1),
            CompletedAtUtc = completedAt,
            AllocatedAt = completedAt.AddMinutes(-1)
        };

    private static DeliveryOrder CreateOrder(string name, OrderPriority priority = OrderPriority.High) =>
        new()
        {
            CustomerName = name,
            DestinationX = 1,
            DestinationY = 0,
            PackageWeightKg = 1,
            Priority = priority,
            Status = OrderStatus.Delivered,
            QueueStatus = OrderQueueStatus.Completed
        };
}
