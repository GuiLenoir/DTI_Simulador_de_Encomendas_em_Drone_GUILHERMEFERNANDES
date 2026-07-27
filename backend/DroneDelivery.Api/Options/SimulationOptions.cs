namespace DroneDelivery.Api.Options;

public sealed class SimulationOptions
{
    public int LoadingDurationSeconds { get; set; } = 3;
    public int DeliveryDurationSeconds { get; set; } = 3;
    public decimal SecondsPerKilometer { get; set; } = 2m;
    public decimal ChargingPercentagePointsPerSecond { get; set; } = 2m;
}
