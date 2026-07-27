namespace DroneDelivery.Api.Options;

public sealed class DroneDeliveryOptions
{
    public decimal BaseX { get; set; }
    public decimal BaseY { get; set; }
    public decimal BatteryConsumptionPerKm { get; set; } = 1.5m;
    public decimal BatterySafetyMarginPercentagePoints { get; set; } = 5m;
    public decimal DroneSpeedKmPerHour { get; set; } = 60m;
    public bool RequireReturnToBase { get; set; } = true;
}
