namespace DroneDelivery.Api.Services;

public interface IDistanceService
{
    decimal Calculate(decimal startX, decimal startY, decimal endX, decimal endY);
}
