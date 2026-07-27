using DroneDelivery.Api.Services;

namespace DroneDelivery.Tests;

public sealed class DistanceServiceTests
{
    [Fact]
    public void Calculate_UsesEuclideanDistance()
    {
        var service = new DistanceService();

        var distance = service.Calculate(0, 0, 3, 4);

        Assert.Equal(5m, distance);
    }
}
