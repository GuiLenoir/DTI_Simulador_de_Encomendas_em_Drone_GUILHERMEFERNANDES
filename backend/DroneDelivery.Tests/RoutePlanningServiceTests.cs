using DroneDelivery.Api.Data;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Services;

namespace DroneDelivery.Tests;

public sealed class RoutePlanningServiceTests
{
    [Fact]
    public async Task CalculateDistanceAsync_UsesDirectRouteWhenNoObstacleExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);

        var distance = await service.CalculateDistanceAsync(
            new RoutePoint(0, 0),
            new[] { new RoutePoint(3, 4) },
            new RoutePoint(0, 0),
            CancellationToken.None);

        Assert.Equal(10m, distance);
    }

    [Fact]
    public async Task CalculateDistanceAsync_UsesDetourWhenActiveNoFlyZoneBlocksRoute()
    {
        await using var dbContext = TestDbContextFactory.Create();
        AddZone(dbContext, isActive: true, new RoutePoint(2, -1), new RoutePoint(4, -1), new RoutePoint(4, 1), new RoutePoint(2, 1));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var distance = await service.CalculateDistanceAsync(
            new RoutePoint(0, 0),
            new[] { new RoutePoint(6, 0) },
            new RoutePoint(0, 0),
            CancellationToken.None);

        Assert.True(distance > 12m);
    }

    [Fact]
    public async Task CalculateDistanceAsync_IgnoresInactiveNoFlyZones()
    {
        await using var dbContext = TestDbContextFactory.Create();
        AddZone(dbContext, isActive: false, new RoutePoint(2, -1), new RoutePoint(4, -1), new RoutePoint(4, 1), new RoutePoint(2, 1));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var distance = await service.CalculateDistanceAsync(
            new RoutePoint(0, 0),
            new[] { new RoutePoint(6, 0) },
            new RoutePoint(0, 0),
            CancellationToken.None);

        Assert.Equal(12m, distance);
    }

    [Fact]
    public async Task CalculateDistanceAsync_RejectsRoutePointInsideNoFlyZone()
    {
        await using var dbContext = TestDbContextFactory.Create();
        AddZone(dbContext, isActive: true, new RoutePoint(-1, -1), new RoutePoint(1, -1), new RoutePoint(1, 1), new RoutePoint(-1, 1));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CalculateDistanceAsync(new RoutePoint(0, 0), new[] { new RoutePoint(3, 0) }, new RoutePoint(0, 0), CancellationToken.None));

        Assert.Equal("ROUTE_BLOCKED_BY_NO_FLY_ZONE", exception.Code);
    }

    [Fact]
    public async Task IsPointInsideActiveNoFlyZoneAsync_TreatsBoundaryAsBlocked()
    {
        await using var dbContext = TestDbContextFactory.Create();
        AddZone(dbContext, isActive: true, new RoutePoint(1, 1), new RoutePoint(3, 1), new RoutePoint(3, 3), new RoutePoint(1, 3));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var inside = await service.IsPointInsideActiveNoFlyZoneAsync(new RoutePoint(2, 2), CancellationToken.None);
        var boundary = await service.IsPointInsideActiveNoFlyZoneAsync(new RoutePoint(1, 2), CancellationToken.None);
        var outside = await service.IsPointInsideActiveNoFlyZoneAsync(new RoutePoint(4, 2), CancellationToken.None);

        Assert.True(inside);
        Assert.True(boundary);
        Assert.False(outside);
    }

    private static RoutePlanningService CreateService(DroneDeliveryDbContext dbContext) =>
        new(dbContext, new DistanceService());

    private static void AddZone(DroneDeliveryDbContext dbContext, bool isActive, params RoutePoint[] points)
    {
        var zone = new NoFlyZone { Name = "Restricted", IsActive = isActive };
        for (var index = 0; index < points.Length; index++)
        {
            zone.Points.Add(new NoFlyZonePoint { Sequence = index + 1, X = points[index].X, Y = points[index].Y });
        }

        dbContext.NoFlyZones.Add(zone);
    }
}
