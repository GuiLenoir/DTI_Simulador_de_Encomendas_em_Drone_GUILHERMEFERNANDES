namespace DroneDelivery.Api.Models;

public sealed class Trip
{
    public int Id { get; set; }
    public int DroneId { get; set; }
    public Drone Drone { get; set; } = null!;
    public TripStatus Status { get; set; } = TripStatus.Planned;
    public DateTime PlannedAtUtc { get; set; }
    public DateTime LoadingStartedAtUtc { get; set; }
    public DateTime FlyingStartedAtUtc { get; set; }
    public DateTime DeliveringStartedAtUtc { get; set; }
    public DateTime ReturningStartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public decimal TotalWeightKg { get; set; }
    public decimal EstimatedDistanceKm { get; set; }
    public decimal EstimatedBatteryConsumptionPercentagePoints { get; set; }
    public decimal BatterySafetyMarginPercentagePoints { get; set; }
    public decimal MinimumRequiredBatteryPercentage { get; set; }
    public decimal BatteryAtDeparturePercentage { get; set; }
    public decimal ExpectedBatteryAtReturnPercentage { get; set; }
    public int LoadingDurationSeconds { get; set; }
    public int OutboundFlightDurationSeconds { get; set; }
    public int DeliveryDurationSeconds { get; set; }
    public int ReturnFlightDurationSeconds { get; set; }
    public ICollection<TripOrder> TripOrders { get; set; } = new List<TripOrder>();
}
