namespace DroneDelivery.Api.Models;

public sealed class DeliveryOrder
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal DestinationX { get; set; }
    public decimal DestinationY { get; set; }
    public decimal PackageWeightKg { get; set; }
    public OrderPriority Priority { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public OrderQueueStatus QueueStatus { get; set; } = OrderQueueStatus.NotQueued;
    public DateTime? QueuedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Delivery? Delivery { get; set; }
    public ICollection<TripOrder> TripOrders { get; set; } = new List<TripOrder>();
}
