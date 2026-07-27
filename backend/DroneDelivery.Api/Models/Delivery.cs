namespace DroneDelivery.Api.Models;

public sealed class Delivery
{
    public int Id { get; set; }
    public int DroneId { get; set; }
    public Drone Drone { get; set; } = null!;
    public int OrderId { get; set; }
    public DeliveryOrder Order { get; set; } = null!;
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Allocated;
    public decimal StartX { get; set; }
    public decimal StartY { get; set; }
    public decimal DestinationX { get; set; }
    public decimal DestinationY { get; set; }
    public decimal EndX { get; set; }
    public decimal EndY { get; set; }
    public decimal EstimatedDistanceKm { get; set; }
    public decimal EstimatedBatteryConsumptionPercent { get; set; }
    public decimal EstimatedDurationMinutes { get; set; }
    public DateTime AllocatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LoadingStartedAtUtc { get; set; }
    public DateTime FlyingStartedAtUtc { get; set; }
    public DateTime DeliveringStartedAtUtc { get; set; }
    public DateTime ReturningStartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public int LoadingDurationSeconds { get; set; }
    public int OutboundFlightDurationSeconds { get; set; }
    public int DeliveryDurationSeconds { get; set; }
    public int ReturnFlightDurationSeconds { get; set; }
}
