namespace DroneDelivery.Api.Models;

public sealed class Drone
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MaxPackageWeightKg { get; set; }
    public decimal MaxRangeKm { get; set; }
    public decimal BatteryLevelPercent { get; set; }
    public decimal BatterySafetyMarginPercentagePoints { get; set; } = 5m;
    public DateTime? ChargingStartedAtUtc { get; set; }
    public decimal? BatteryAtChargingStartPercentage { get; set; }
    public decimal ChargingRatePercentagePointsPerSecond { get; set; } = 2m;
    public DateTime? ChargingCompletedAtUtc { get; set; }
    public decimal CurrentX { get; set; }
    public decimal CurrentY { get; set; }
    public decimal AverageSpeedKmPerHour { get; set; } = 60m;
    public decimal BatteryConsumptionPercentagePerKm { get; set; } = 1.5m;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DroneStatus Status { get; set; } = DroneStatus.Idle;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
