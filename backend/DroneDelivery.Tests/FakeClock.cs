using DroneDelivery.Api.Services;

namespace DroneDelivery.Tests;

internal sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
}
