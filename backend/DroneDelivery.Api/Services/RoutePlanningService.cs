using DroneDelivery.Api.Data;
using DroneDelivery.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DroneDelivery.Api.Services;

public sealed class RoutePlanningService : IRoutePlanningService
{
    private const decimal Epsilon = 0.0001m;
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IDistanceService _distanceService;

    public RoutePlanningService(DroneDeliveryDbContext dbContext, IDistanceService distanceService)
    {
        _dbContext = dbContext;
        _distanceService = distanceService;
    }

    public async Task<decimal> CalculateDistanceAsync(
        RoutePoint start,
        IReadOnlyList<RoutePoint> stops,
        RoutePoint end,
        CancellationToken cancellationToken)
    {
        var polygons = await LoadActivePolygonsAsync(cancellationToken);

        var distance = 0m;
        var current = start;
        foreach (var stop in stops)
        {
            distance += CalculateSegmentDistance(current, stop, polygons);
            current = stop;
        }

        distance += CalculateSegmentDistance(current, end, polygons);
        return Math.Round(distance, 2);
    }

    public async Task<bool> IsPointInsideActiveNoFlyZoneAsync(RoutePoint point, CancellationToken cancellationToken)
    {
        var polygons = await LoadActivePolygonsAsync(cancellationToken);
        return polygons.Any(polygon => IsPointStrictlyInsidePolygon(point, polygon) || IsPointOnPolygonBoundary(point, polygon));
    }

    private async Task<IReadOnlyList<IReadOnlyList<RoutePoint>>> LoadActivePolygonsAsync(CancellationToken cancellationToken)
    {
        var zones = await _dbContext.NoFlyZones
            .Include(zone => zone.Points)
            .Where(zone => zone.IsActive)
            .ToListAsync(cancellationToken);

        return zones
            .Select(zone => zone.Points.OrderBy(point => point.Sequence).Select(point => new RoutePoint(point.X, point.Y)).ToList())
            .Where(points => points.Count >= 3)
            .ToList();
    }

    private decimal CalculateSegmentDistance(RoutePoint start, RoutePoint end, IReadOnlyList<IReadOnlyList<RoutePoint>> polygons)
    {
        if (polygons.Count == 0 || IsSegmentValid(start, end, polygons))
        {
            return Distance(start, end);
        }

        return FindShortestVisiblePath(start, end, polygons);
    }

    private decimal FindShortestVisiblePath(RoutePoint start, RoutePoint end, IReadOnlyList<IReadOnlyList<RoutePoint>> polygons)
    {
        var nodes = new List<RoutePoint> { start, end };
        nodes.AddRange(polygons.SelectMany(points => points));
        nodes = nodes.Distinct().ToList();

        var distances = nodes.ToDictionary(node => node, node => decimal.MaxValue);
        var visited = new HashSet<RoutePoint>();
        distances[start] = 0m;

        while (visited.Count < nodes.Count)
        {
            var current = nodes
                .Where(node => !visited.Contains(node))
                .OrderBy(node => distances[node])
                .ThenBy(node => node.X)
                .ThenBy(node => node.Y)
                .FirstOrDefault();

            if (current is null || distances[current] == decimal.MaxValue)
            {
                break;
            }

            if (current == end)
            {
                return Math.Round(distances[end], 2);
            }

            visited.Add(current);
            foreach (var next in nodes.Where(node => !visited.Contains(node)))
            {
                if (!IsSegmentValid(current, next, polygons))
                {
                    continue;
                }

                var candidate = distances[current] + Distance(current, next);
                if (candidate < distances[next])
                {
                    distances[next] = candidate;
                }
            }
        }

        throw new ValidationException("NO_VALID_ROUTE_AVAILABLE", "No valid route available", "The route cannot avoid the active no-fly zones.");
    }

    private bool IsSegmentValid(RoutePoint start, RoutePoint end, IReadOnlyList<IReadOnlyList<RoutePoint>> polygons)
    {
        foreach (var polygon in polygons)
        {
            if (IsPointStrictlyInsidePolygon(start, polygon) || IsPointStrictlyInsidePolygon(end, polygon))
            {
                throw new ValidationException("ROUTE_BLOCKED_BY_NO_FLY_ZONE", "Route blocked by no-fly zone", "A route point is inside an active no-fly zone.");
            }

            for (var index = 0; index < polygon.Count; index++)
            {
                var edgeStart = polygon[index];
                var edgeEnd = polygon[(index + 1) % polygon.Count];
                if (SegmentsIntersect(start, end, edgeStart, edgeEnd) &&
                    !TouchesAllowedEndpoint(start, end, edgeStart, edgeEnd))
                {
                    return false;
                }
            }

            var midpoint = new RoutePoint((start.X + end.X) / 2m, (start.Y + end.Y) / 2m);
            if (IsPointStrictlyInsidePolygon(midpoint, polygon))
            {
                return false;
            }
        }

        return true;
    }

    private decimal Distance(RoutePoint a, RoutePoint b) =>
        _distanceService.Calculate(a.X, a.Y, b.X, b.Y);

    private static bool IsPointStrictlyInsidePolygon(RoutePoint point, IReadOnlyList<RoutePoint> polygon)
    {
        if (IsPointOnPolygonBoundary(point, polygon))
        {
            return false;
        }

        return IsPointInsidePolygon(point, polygon);
    }

    private static bool IsPointOnPolygonBoundary(RoutePoint point, IReadOnlyList<RoutePoint> polygon)
    {
        for (var index = 0; index < polygon.Count; index++)
        {
            if (OnSegment(polygon[index], point, polygon[(index + 1) % polygon.Count]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointInsidePolygon(RoutePoint point, IReadOnlyList<RoutePoint> polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];
            var crosses = ((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X;
            if (crosses)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool SegmentsIntersect(RoutePoint a, RoutePoint b, RoutePoint c, RoutePoint d)
    {
        var o1 = Orientation(a, b, c);
        var o2 = Orientation(a, b, d);
        var o3 = Orientation(c, d, a);
        var o4 = Orientation(c, d, b);

        if (o1 != o2 && o3 != o4)
        {
            return true;
        }

        return o1 == 0 && OnSegment(a, c, b) ||
               o2 == 0 && OnSegment(a, d, b) ||
               o3 == 0 && OnSegment(c, a, d) ||
               o4 == 0 && OnSegment(c, b, d);
    }

    private static bool TouchesAllowedEndpoint(RoutePoint a, RoutePoint b, RoutePoint c, RoutePoint d) =>
        SamePoint(a, c) || SamePoint(a, d) || SamePoint(b, c) || SamePoint(b, d);

    private static int Orientation(RoutePoint a, RoutePoint b, RoutePoint c)
    {
        var value = (b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y);
        if (Math.Abs(value) <= Epsilon)
        {
            return 0;
        }

        return value > 0 ? 1 : 2;
    }

    private static bool OnSegment(RoutePoint a, RoutePoint b, RoutePoint c) =>
        b.X <= Math.Max(a.X, c.X) + Epsilon &&
        b.X + Epsilon >= Math.Min(a.X, c.X) &&
        b.Y <= Math.Max(a.Y, c.Y) + Epsilon &&
        b.Y + Epsilon >= Math.Min(a.Y, c.Y);

    private static bool SamePoint(RoutePoint a, RoutePoint b) =>
        Math.Abs(a.X - b.X) <= Epsilon && Math.Abs(a.Y - b.Y) <= Epsilon;
}
