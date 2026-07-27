namespace DroneDelivery.Api.Services;

public interface IRoutePlanningService
{
    Task<decimal> CalculateDistanceAsync(
        RoutePoint start,
        IReadOnlyList<RoutePoint> stops,
        RoutePoint end,
        CancellationToken cancellationToken);

    Task<bool> IsPointInsideActiveNoFlyZoneAsync(
        RoutePoint point,
        CancellationToken cancellationToken);
}

public sealed record RoutePoint(decimal X, decimal Y);
