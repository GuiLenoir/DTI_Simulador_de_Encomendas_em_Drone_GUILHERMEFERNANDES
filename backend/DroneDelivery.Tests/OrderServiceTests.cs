using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Services;

namespace DroneDelivery.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsInvalidWeight()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateOrderRequest("Customer", 1, 1, 0, OrderPriority.High), CancellationToken.None));

        Assert.Equal("INVALID_PACKAGE_WEIGHT", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDestinationInsideActiveNoFlyZone()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var zone = new NoFlyZone { Name = "Restricted", IsActive = true };
        zone.Points.Add(new NoFlyZonePoint { Sequence = 1, X = 1, Y = 1 });
        zone.Points.Add(new NoFlyZonePoint { Sequence = 2, X = 3, Y = 1 });
        zone.Points.Add(new NoFlyZonePoint { Sequence = 3, X = 3, Y = 3 });
        zone.Points.Add(new NoFlyZonePoint { Sequence = 4, X = 1, Y = 3 });
        dbContext.NoFlyZones.Add(zone);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateOrderRequest("Customer", 2, 2, 1, OrderPriority.High), CancellationToken.None));

        Assert.Equal("ORDER_DESTINATION_IN_NO_FLY_ZONE", exception.Code);
    }

    [Fact]
    public async Task GetQueueAsync_SortsByPriorityThenCreationTimeThenDistance()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Orders.AddRange(
            new DeliveryOrder { CustomerName = "Low", DestinationX = 1, DestinationY = 1, PackageWeightKg = 1, Priority = OrderPriority.Low, Status = OrderStatus.Pending, QueueStatus = OrderQueueStatus.Queued },
            new DeliveryOrder { CustomerName = "High old", DestinationX = 5, DestinationY = 0, PackageWeightKg = 1, Priority = OrderPriority.High, Status = OrderStatus.Pending, QueueStatus = OrderQueueStatus.Queued },
            new DeliveryOrder { CustomerName = "High new", DestinationX = 1, DestinationY = 0, PackageWeightKg = 1, Priority = OrderPriority.High, Status = OrderStatus.Pending, QueueStatus = OrderQueueStatus.Queued },
            new DeliveryOrder { CustomerName = "Medium", DestinationX = 1, DestinationY = 0, PackageWeightKg = 1, Priority = OrderPriority.Medium, Status = OrderStatus.Pending, QueueStatus = OrderQueueStatus.Queued });
        await dbContext.SaveChangesAsync();

        var highOld = dbContext.Orders.Single(order => order.CustomerName == "High old");
        var highNew = dbContext.Orders.Single(order => order.CustomerName == "High new");
        var low = dbContext.Orders.Single(order => order.CustomerName == "Low");
        var medium = dbContext.Orders.Single(order => order.CustomerName == "Medium");
        highOld.CreatedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        highNew.CreatedAt = new DateTime(2026, 1, 1, 9, 10, 0, DateTimeKind.Utc);
        medium.CreatedAt = new DateTime(2026, 1, 1, 8, 30, 0, DateTimeKind.Utc);
        low.CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        highOld.QueuedAtUtc = highOld.CreatedAt;
        highNew.QueuedAtUtc = highNew.CreatedAt;
        medium.QueuedAtUtc = medium.CreatedAt;
        low.QueuedAtUtc = low.CreatedAt;
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var queue = await service.GetQueueAsync(CancellationToken.None);

        Assert.Collection(queue,
            item => Assert.Equal("High old", item.CustomerName),
            item => Assert.Equal("High new", item.CustomerName),
            item => Assert.Equal("Medium", item.CustomerName),
            item => Assert.Equal("Low", item.CustomerName));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDeliveredStatusForCompletedTimeline()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var drone = new Drone
        {
            Code = "Drone",
            MaxPackageWeightKg = 10,
            MaxRangeKm = 100,
            BatteryLevelPercent = 100,
            Status = DroneStatus.Returning
        };
        var order = new DeliveryOrder
        {
            CustomerName = "Completed order",
            DestinationX = 3,
            DestinationY = 4,
            PackageWeightKg = 1,
            Priority = OrderPriority.High,
            Status = OrderStatus.InTransit
        };
        dbContext.Drones.Add(drone);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var clock = new FakeClock();
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
            EstimatedBatteryConsumptionPercent = 15,
            EstimatedDurationMinutes = 10,
            AllocatedAt = clock.UtcNow.AddMinutes(-10),
            CreatedAtUtc = clock.UtcNow.AddMinutes(-10),
            LoadingStartedAtUtc = clock.UtcNow.AddMinutes(-10),
            FlyingStartedAtUtc = clock.UtcNow.AddMinutes(-9),
            DeliveringStartedAtUtc = clock.UtcNow.AddMinutes(-8),
            ReturningStartedAtUtc = clock.UtcNow.AddMinutes(-7),
            CompletedAtUtc = clock.UtcNow.AddMinutes(-1),
            LoadingDurationSeconds = 60,
            OutboundFlightDurationSeconds = 60,
            DeliveryDurationSeconds = 60,
            ReturnFlightDurationSeconds = 60
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, clock);

        var orders = await service.GetAllAsync(CancellationToken.None);

        var response = Assert.Single(orders);
        Assert.Equal(OrderStatus.Delivered, response.Status);
    }

    private static OrderService CreateService(DroneDelivery.Api.Data.DroneDeliveryDbContext dbContext, FakeClock? clock = null) =>
        new(
            dbContext,
            new DistanceService(),
            new RoutePlanningService(dbContext, new DistanceService()),
            new DeliveryStateService(),
            new TripStateService(),
            clock ?? new FakeClock());
}
