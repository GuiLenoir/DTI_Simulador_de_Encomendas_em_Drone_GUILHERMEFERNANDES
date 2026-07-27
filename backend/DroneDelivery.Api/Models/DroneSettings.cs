namespace DroneDelivery.Api.Models;

public sealed class DroneSettings
{
    public int Id { get; set; }
    public decimal BatterySafetyMarginPercentagePoints { get; set; } = 5m;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
