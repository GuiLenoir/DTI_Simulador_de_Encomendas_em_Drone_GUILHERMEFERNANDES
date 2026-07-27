using DroneDelivery.Api.Models;
using DroneDelivery.Api.Services;

namespace DroneDelivery.Tests;

public sealed class DeliveryStateServiceTests
{
    private readonly DeliveryStateService _service = new();
    private readonly DateTime _start = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GetCurrentState_BeforeFlyingStarts_ReturnsLoading()
    {
        var delivery = CreateDelivery();

        var state = _service.GetCurrentState(delivery, _start.AddSeconds(2));

        Assert.Equal(DroneStatus.Loading, state.DroneStatus);
        Assert.Equal("Loading", state.CurrentPhase);
    }

    [Fact]
    public void GetCurrentState_WhenFlyingStarts_ReturnsFlying()
    {
        var delivery = CreateDelivery();

        var state = _service.GetCurrentState(delivery, delivery.FlyingStartedAtUtc);

        Assert.Equal(DroneStatus.Flying, state.DroneStatus);
        Assert.Equal("Flying", state.CurrentPhase);
    }

    [Fact]
    public void GetCurrentState_WhenDeliveringStarts_ReturnsDelivering()
    {
        var delivery = CreateDelivery();

        var state = _service.GetCurrentState(delivery, delivery.DeliveringStartedAtUtc);

        Assert.Equal(DroneStatus.Delivering, state.DroneStatus);
        Assert.Equal("Delivering", state.CurrentPhase);
    }

    [Fact]
    public void GetCurrentState_WhenReturningStarts_ReturnsReturning()
    {
        var delivery = CreateDelivery();

        var state = _service.GetCurrentState(delivery, delivery.ReturningStartedAtUtc);

        Assert.Equal(DroneStatus.Returning, state.DroneStatus);
        Assert.Equal("Returning", state.CurrentPhase);
    }

    [Fact]
    public void GetCurrentState_WhenCompleted_ReturnsDeliveredAndIdle()
    {
        var delivery = CreateDelivery();

        var state = _service.GetCurrentState(delivery, delivery.CompletedAtUtc);

        Assert.Equal(DroneStatus.Idle, state.DroneStatus);
        Assert.Equal(DeliveryStatus.Delivered, state.DeliveryStatus);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void GetCurrentState_ProgressRemainsBetweenZeroAndOneHundred()
    {
        var delivery = CreateDelivery();

        var beforeStart = _service.GetCurrentState(delivery, _start.AddMinutes(-1));
        var afterCompletion = _service.GetCurrentState(delivery, _start.AddMinutes(1));

        Assert.InRange(beforeStart.ProgressPercentage, 0, 100);
        Assert.InRange(afterCompletion.ProgressPercentage, 0, 100);
    }

    [Fact]
    public void GetCurrentState_RemainingSecondsNeverNegative()
    {
        var delivery = CreateDelivery();

        var state = _service.GetCurrentState(delivery, _start.AddMinutes(1));

        Assert.Equal(0, state.RemainingPhaseSeconds);
    }

    private Delivery CreateDelivery() =>
        new()
        {
            CreatedAtUtc = _start,
            LoadingStartedAtUtc = _start,
            FlyingStartedAtUtc = _start.AddSeconds(3),
            DeliveringStartedAtUtc = _start.AddSeconds(7),
            ReturningStartedAtUtc = _start.AddSeconds(10),
            CompletedAtUtc = _start.AddSeconds(14)
        };
}
