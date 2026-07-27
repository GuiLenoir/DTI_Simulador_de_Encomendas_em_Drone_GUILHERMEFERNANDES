namespace DroneDelivery.Api.Services;

public sealed class DistanceService : IDistanceService
{
    public decimal Calculate(decimal startX, decimal startY, decimal endX, decimal endY)
    {
        var deltaX = (double)(endX - startX);
        var deltaY = (double)(endY - startY);
        return Math.Round((decimal)Math.Sqrt(deltaX * deltaX + deltaY * deltaY), 2);
    }
}
