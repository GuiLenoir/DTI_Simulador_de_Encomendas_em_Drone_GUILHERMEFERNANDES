namespace DroneDelivery.Api.Models;

public sealed class NoFlyZone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<NoFlyZonePoint> Points { get; } = new List<NoFlyZonePoint>();
}
