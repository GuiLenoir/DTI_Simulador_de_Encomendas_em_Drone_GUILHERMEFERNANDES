using DroneDelivery.Api.Models;

namespace DroneDelivery.Tests;

public sealed class DroneDeliveryDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_DoesNotTouchUnchangedTrackedEntities()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var updatedAt = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        dbContext.Drones.Add(new Drone
        {
            Code = "DRN",
            Name = "Drone",
            MaxPackageWeightKg = 10,
            MaxRangeKm = 100,
            BatteryLevelPercent = 100,
            AverageSpeedKmPerHour = 60,
            BatteryConsumptionPercentagePerKm = 2.5m,
            UpdatedAt = updatedAt,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
        var savedUpdatedAt = dbContext.Drones.Single().UpdatedAt;

        await dbContext.SaveChangesAsync();

        Assert.Equal(savedUpdatedAt, dbContext.Drones.Single().UpdatedAt);
    }
}
