using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneDelivery.Api.Services;

public sealed class CustomerSimulationService : ICustomerSimulationService
{
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IOrderService _orderService;
    private readonly IDeliveryPlanningService _planningService;
    private readonly ITripStateService _tripStateService;
    private readonly IDeliveryStateService _deliveryStateService;
    private readonly IDistanceService _distanceService;
    private readonly IClock _clock;

    public CustomerSimulationService(
        DroneDeliveryDbContext dbContext,
        IOrderService orderService,
        IDeliveryPlanningService planningService,
        ITripStateService tripStateService,
        IDeliveryStateService deliveryStateService,
        IDistanceService distanceService,
        IClock clock)
    {
        _dbContext = dbContext;
        _orderService = orderService;
        _planningService = planningService;
        _tripStateService = tripStateService;
        _deliveryStateService = deliveryStateService;
        _distanceService = distanceService;
        _clock = clock;
    }

    public async Task<CustomerOrderCreatedResponse> CreateOrderAsync(CustomerOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(
            new CreateOrderRequest(request.CustomerName, request.DestinationX, request.DestinationY, request.PackageWeightKg, request.Priority),
            cancellationToken);
        await _orderService.QueueAsync(order.Id, cancellationToken);
        await _planningService.ProcessQueueAsync(cancellationToken);
        return new CustomerOrderCreatedResponse(order.Id, $"PED-{order.Id}");
    }

    public async Task<CustomerTrackingResponse> GetTrackingAsync(int orderId, CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        await _planningService.ProcessQueueAsync(cancellationToken);
        var order = await _dbContext.Orders.FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new NotFoundException($"Order {orderId} was not found.");

        var trip = await _dbContext.Trips
            .Include(item => item.Drone)
            .Include(item => item.TripOrders)
            .ThenInclude(item => item.Order)
            .Where(item => item.TripOrders.Any(tripOrder => tripOrder.OrderId == orderId))
            .OrderByDescending(item => item.PlannedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (trip is not null)
        {
            return MapTripTracking(order, trip, utcNow);
        }

        var delivery = await _dbContext.Deliveries
            .Include(item => item.Drone)
            .Include(item => item.Order)
            .FirstOrDefaultAsync(item => item.OrderId == orderId, cancellationToken);
        return delivery is null
            ? MapPendingTracking(order)
            : MapDeliveryTracking(order, delivery, utcNow);
    }

    private CustomerTrackingResponse MapTripTracking(DeliveryOrder order, Trip trip, DateTime utcNow)
    {
        var state = _tripStateService.GetCurrentState(trip, utcNow);
        var trackedTripOrder = trip.TripOrders.First(item => item.OrderId == order.Id);
        var receivedAtUtc = trackedTripOrder.EstimatedArrivalAtUtc;
        var route = BuildTripRouteToCustomer(trip, order.Id);
        var position = Interpolate(route, trip.FlyingStartedAtUtc, receivedAtUtc, utcNow);
        var remainingDistance = CalculateRemainingDistance(route, position);
        var isReceived = utcNow >= receivedAtUtc;
        var friendlyStatus = isReceived ? "Entrega concluída" : GetFriendlyTripStatus(state.TripStatus);

        return new CustomerTrackingResponse(
            order.Id,
            $"PED-{order.Id}",
            friendlyStatus,
            isReceived ? "Received" : state.TripStatus.ToString(),
            trip.Drone.Code,
            trip.Id,
            null,
            order.Priority,
            order.PackageWeightKg,
            new RoutePointResponse(order.DestinationX, order.DestinationY),
            route,
            trip.LoadingStartedAtUtc,
            receivedAtUtc,
            CalculateProgressPercentage(trip.LoadingStartedAtUtc, receivedAtUtc, utcNow),
            isReceived ? 0 : remainingDistance,
            new RoutePointResponse(position.X, position.Y),
            GetFeedback(friendlyStatus, remainingDistance, trip.Drone.Code));
    }

    private CustomerTrackingResponse MapDeliveryTracking(DeliveryOrder order, Delivery delivery, DateTime utcNow)
    {
        var state = _deliveryStateService.GetCurrentState(delivery, utcNow);
        var route = new[]
        {
            new CustomerRoutePointResponse(0, "Base", null, null, null, null, delivery.StartX, delivery.StartY),
            new CustomerRoutePointResponse(1, "CustomerDestination", order.Id, $"PED-{order.Id}", order.Priority, order.PackageWeightKg, order.DestinationX, order.DestinationY)
        };
        var receivedAtUtc = delivery.ReturningStartedAtUtc;
        var position = Interpolate(route, delivery.FlyingStartedAtUtc, receivedAtUtc, utcNow);
        var remainingDistance = CalculateRemainingDistance(route, position);
        var isReceived = utcNow >= receivedAtUtc;
        var friendlyStatus = isReceived ? "Entrega concluída" : GetFriendlyDeliveryStatus(state.DeliveryStatus);
        return new CustomerTrackingResponse(
            order.Id,
            $"PED-{order.Id}",
            friendlyStatus,
            isReceived ? "Received" : state.DeliveryStatus.ToString(),
            delivery.Drone.Code,
            null,
            delivery.Id,
            order.Priority,
            order.PackageWeightKg,
            new RoutePointResponse(order.DestinationX, order.DestinationY),
            route,
            delivery.LoadingStartedAtUtc,
            receivedAtUtc,
            CalculateProgressPercentage(delivery.LoadingStartedAtUtc, receivedAtUtc, utcNow),
            isReceived ? 0 : remainingDistance,
            new RoutePointResponse(position.X, position.Y),
            GetFeedback(friendlyStatus, remainingDistance, delivery.Drone.Code));
    }

    private CustomerTrackingResponse MapPendingTracking(DeliveryOrder order)
    {
        var route = new[]
        {
            new CustomerRoutePointResponse(0, "Base", null, null, null, null, 0, 0),
            new CustomerRoutePointResponse(1, "CustomerDestination", order.Id, $"PED-{order.Id}", order.Priority, order.PackageWeightKg, order.DestinationX, order.DestinationY)
        };
        var status = order.QueueStatus == OrderQueueStatus.Queued ? "Aguardando planejamento" : "Pedido recebido";
        return new CustomerTrackingResponse(
            order.Id,
            $"PED-{order.Id}",
            status,
            order.QueueStatus.ToString(),
            null,
            null,
            null,
            order.Priority,
            order.PackageWeightKg,
            new RoutePointResponse(order.DestinationX, order.DestinationY),
            route,
            null,
            null,
            0,
            _distanceService.Calculate(0, 0, order.DestinationX, order.DestinationY),
            new RoutePointResponse(0, 0),
            "Estamos procurando o melhor drone para sua entrega.");
    }

    private IReadOnlyList<CustomerRoutePointResponse> BuildTripRouteToCustomer(Trip trip, int trackedOrderId)
    {
        var points = new List<CustomerRoutePointResponse>
        {
            new(0, "Base", null, null, null, null, 0, 0)
        };
        foreach (var item in trip.TripOrders.OrderBy(item => item.DeliverySequence))
        {
            points.Add(new CustomerRoutePointResponse(
                item.DeliverySequence,
                item.OrderId == trackedOrderId ? "CustomerDestination" : "Delivery",
                item.OrderId,
                $"PED-{item.OrderId}",
                item.Order.Priority,
                item.Order.PackageWeightKg,
                item.Order.DestinationX,
                item.Order.DestinationY));
            if (item.OrderId == trackedOrderId)
            {
                break;
            }
        }

        return points;
    }

    private (decimal X, decimal Y) Interpolate(IReadOnlyList<CustomerRoutePointResponse> route, DateTime start, DateTime end, DateTime utcNow)
    {
        if (utcNow <= start || route.Count < 2)
        {
            return (route[0].X, route[0].Y);
        }

        if (utcNow >= end)
        {
            var destination = route.LastOrDefault(point => point.Type == "CustomerDestination") ?? route.Last();
            return (destination.X, destination.Y);
        }

        var totalDistance = CalculateRouteDistance(route);
        if (totalDistance <= 0)
        {
            return (route[0].X, route[0].Y);
        }

        var progress = (decimal)((utcNow - start).TotalSeconds / Math.Max(1, (end - start).TotalSeconds));
        var targetDistance = totalDistance * Math.Clamp(progress, 0, 1);
        var traversed = 0m;
        for (var index = 1; index < route.Count; index++)
        {
            var previous = route[index - 1];
            var current = route[index];
            var segment = _distanceService.Calculate(previous.X, previous.Y, current.X, current.Y);
            if (traversed + segment >= targetDistance)
            {
                var segmentProgress = segment <= 0 ? 1 : (targetDistance - traversed) / segment;
                return (
                    previous.X + (current.X - previous.X) * segmentProgress,
                    previous.Y + (current.Y - previous.Y) * segmentProgress);
            }

            traversed += segment;
        }

        return (route[^1].X, route[^1].Y);
    }

    private decimal CalculateRemainingDistance(IReadOnlyList<CustomerRoutePointResponse> route, (decimal X, decimal Y) position)
    {
        var destination = route.FirstOrDefault(point => point.Type == "CustomerDestination");
        return destination is null ? 0 : Math.Round(_distanceService.Calculate(position.X, position.Y, destination.X, destination.Y), 2);
    }

    private decimal CalculateRouteDistance(IReadOnlyList<CustomerRoutePointResponse> route) =>
        route.Skip(1).Select((point, index) => _distanceService.Calculate(route[index].X, route[index].Y, point.X, point.Y)).Sum();

    private static int CalculateProgressPercentage(DateTime start, DateTime receivedAtUtc, DateTime utcNow)
    {
        var totalSeconds = Math.Max(1, (int)Math.Ceiling((receivedAtUtc - start).TotalSeconds));
        var elapsedSeconds = Math.Clamp((int)Math.Floor((utcNow - start).TotalSeconds), 0, totalSeconds);
        return Math.Clamp((int)Math.Floor(elapsedSeconds / (double)totalSeconds * 100), 0, 100);
    }

    private static string GetFriendlyTripStatus(TripStatus status) =>
        status switch
        {
            TripStatus.Planned => "Sua entrega foi planejada e será iniciada em breve",
            TripStatus.Loading => "Drone sendo preparado",
            TripStatus.Flying => "Seu pacote está a caminho",
            TripStatus.Delivering => "O drone chegou ao destino",
            TripStatus.Returning => "Entrega concluída",
            TripStatus.Completed => "Entrega concluída",
            TripStatus.Cancelled => "Este pedido foi cancelado",
            _ => "Acompanhando pedido"
        };

    private static string GetFriendlyDeliveryStatus(DeliveryStatus status) =>
        status switch
        {
            DeliveryStatus.Allocated => "Drone sendo preparado",
            DeliveryStatus.InTransit => "Seu pacote está a caminho",
            DeliveryStatus.Delivered => "Entrega concluída",
            DeliveryStatus.Failed => "Ainda não foi possível definir uma rota segura para sua entrega",
            _ => "Acompanhando pedido"
        };

    private static string GetFeedback(string status, decimal remainingDistance, string droneCode)
    {
        if (status == "Entrega concluída")
        {
            return "Pedido recebido com sucesso. O drone está retornando à base.";
        }

        if (remainingDistance <= 2.5m)
        {
            return $"Seu pacote está a aproximadamente {Math.Max(1, (int)Math.Round(remainingDistance))} quadras de distância.";
        }

        return $"Seu pedido foi atribuído ao {droneCode}. O drone saiu da base e está a aproximadamente {remainingDistance:0.##} km de distância.";
    }
}
