using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneDelivery.Api.Services;

public sealed class ReportService : IReportService
{
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IDistanceService _distanceService;
    private readonly IClock _clock;

    public ReportService(DroneDeliveryDbContext dbContext, IDistanceService distanceService, IClock clock)
    {
        _dbContext = dbContext;
        _distanceService = distanceService;
        _clock = clock;
    }

    public async Task<ReportResponse> GetAsync(DateTime? from, DateTime? to, int? droneId, OrderPriority? priority, CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var trips = await _dbContext.Trips
            .Include(trip => trip.Drone)
            .Include(trip => trip.TripOrders)
            .ThenInclude(tripOrder => tripOrder.Order)
            .Where(trip => trip.CompletedAtUtc <= utcNow)
            .ToListAsync(cancellationToken);
        var deliveries = await _dbContext.Deliveries
            .Include(delivery => delivery.Drone)
            .Include(delivery => delivery.Order)
            .Where(delivery => delivery.CompletedAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

        var journeyRows = trips.Select(MapTrip).Concat(deliveries.Select(MapDelivery))
            .Where(row => (!from.HasValue || row.CompletedAtUtc >= from.Value) &&
                (!to.HasValue || row.CompletedAtUtc <= to.Value) &&
                (!droneId.HasValue || row.DroneId == droneId.Value) &&
                (!priority.HasValue || row.Points.Any(point => point.Priority == priority.Value)))
            .OrderBy(row => row.CompletedAtUtc)
            .ThenBy(row => row.Id)
            .ToList();

        var completedPoints = journeyRows.SelectMany(row => row.Points.Where(point => point.Type != "Base")).ToList();
        var durations = journeyRows.SelectMany(GetDeliveryDurations).Where(seconds => seconds > 0).ToList();
        var summary = new DeliverySummaryResponse(
            completedPoints.Count,
            durations.Count == 0 ? 0 : (int)Math.Round(durations.Average()));

        var efficiency = journeyRows
            .GroupBy(row => new { row.DroneId, row.DroneCode })
            .Select(group =>
            {
                var completed = group.Sum(row => row.Points.Count(point => point.Type != "Base"));
                var weight = group.Sum(row => row.Points.Where(point => point.Type != "Base").Sum(point => point.WeightKg ?? 0));
                var distance = group.Sum(row => row.DistanceKm);
                var battery = group.Sum(row => row.BatteryConsumed);
                var denominator = distance + battery;
                return new DroneEfficiencyResponse(
                    group.Key.DroneId,
                    group.Key.DroneCode,
                    completed,
                    Math.Round(weight, 2),
                    Math.Round(distance, 2),
                    Math.Round(battery, 2),
                    denominator <= 0 ? 0 : Math.Round((completed + weight) / denominator, 4));
            })
            .Where(item => item.CompletedDeliveries > 0 && item.EfficiencyScore > 0)
            .OrderByDescending(item => item.EfficiencyScore)
            .ThenBy(item => item.DroneCode)
            .FirstOrDefault();

        var map = new DeliveryMapResponse(
            completedPoints.Count,
            journeyRows.Select(row => row.DroneId).Distinct().Count(),
            Math.Round(journeyRows.Sum(row => row.DistanceKm), 2),
            journeyRows.Select(row => row.Response).ToList());

        return new ReportResponse(summary, efficiency, map);
    }

    private ReportJourneyRow MapTrip(Trip trip)
    {
        var points = new List<DeliveryMapPointResponse>
        {
            new(0, "Base", null, null, null, null, 0, 0, null)
        };
        points.AddRange(trip.TripOrders
            .OrderBy(item => item.DeliverySequence)
            .Select(item => new DeliveryMapPointResponse(
                item.DeliverySequence,
                "Delivery",
                item.OrderId,
                $"PED-{item.OrderId}",
                item.Order.Priority,
                item.Order.PackageWeightKg,
                item.Order.DestinationX,
                item.Order.DestinationY,
                item.DeliveryCompletedAtUtc)));

        return new ReportJourneyRow(
            $"trip-{trip.Id}",
            trip.DroneId,
            trip.Drone.Code,
            trip.CompletedAtUtc,
            trip.EstimatedDistanceKm,
            trip.EstimatedBatteryConsumptionPercentagePoints,
            points,
            new DeliveryMapJourneyResponse($"trip-{trip.Id}", trip.Id, null, trip.DroneId, trip.Drone.Code, trip.CompletedAtUtc, trip.EstimatedDistanceKm, points),
            trip.LoadingStartedAtUtc);
    }

    private ReportJourneyRow MapDelivery(Delivery delivery)
    {
        var points = new List<DeliveryMapPointResponse>
        {
            new(0, "Base", null, null, null, null, delivery.StartX, delivery.StartY, null),
            new(1, "Delivery", delivery.OrderId, $"PED-{delivery.OrderId}", delivery.Order.Priority, delivery.Order.PackageWeightKg, delivery.DestinationX, delivery.DestinationY, delivery.CompletedAtUtc)
        };
        return new ReportJourneyRow(
            $"delivery-{delivery.Id}",
            delivery.DroneId,
            delivery.Drone.Code,
            delivery.CompletedAtUtc,
            delivery.EstimatedDistanceKm,
            delivery.EstimatedBatteryConsumptionPercent,
            points,
            new DeliveryMapJourneyResponse($"delivery-{delivery.Id}", null, delivery.Id, delivery.DroneId, delivery.Drone.Code, delivery.CompletedAtUtc, delivery.EstimatedDistanceKm, points),
            delivery.LoadingStartedAtUtc);
    }

    private IEnumerable<double> GetDeliveryDurations(ReportJourneyRow row)
    {
        foreach (var point in row.Points.Where(point => point.CompletedAtUtc.HasValue && point.Type != "Base"))
        {
            yield return (point.CompletedAtUtc!.Value - row.StartedAtUtc).TotalSeconds;
        }
    }

    private sealed record ReportJourneyRow(
        string Id,
        int DroneId,
        string DroneCode,
        DateTime CompletedAtUtc,
        decimal DistanceKm,
        decimal BatteryConsumed,
        IReadOnlyList<DeliveryMapPointResponse> Points,
        DeliveryMapJourneyResponse Response,
        DateTime StartedAtUtc);
}
