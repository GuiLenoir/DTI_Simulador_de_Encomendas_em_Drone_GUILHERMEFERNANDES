namespace DroneDelivery.Api.Models;

public sealed class TripOrder
{
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;
    public int OrderId { get; set; }
    public DeliveryOrder Order { get; set; } = null!;
    public int DeliverySequence { get; set; }
    public DateTime EstimatedArrivalAtUtc { get; set; }
    public DateTime DeliveryStartedAtUtc { get; set; }
    public DateTime DeliveryCompletedAtUtc { get; set; }
}
