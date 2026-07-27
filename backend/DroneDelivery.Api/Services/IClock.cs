namespace DroneDelivery.Api.Services;

public interface IClock
{
    DateTime UtcNow { get; }
}
