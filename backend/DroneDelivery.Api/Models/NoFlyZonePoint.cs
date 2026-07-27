namespace DroneDelivery.Api.Models;

public sealed class NoFlyZonePoint
{
    public int Id { get; set; }
    public int NoFlyZoneId { get; set; }
    public int Sequence { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }

    public NoFlyZone NoFlyZone { get; set; } = null!;
}
