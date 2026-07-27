using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneDelivery.Api.Services;

public sealed class OrderService : IOrderService
{
    private readonly DroneDeliveryDbContext _dbContext;
    private readonly IDistanceService _distanceService;
    private readonly IRoutePlanningService _routePlanningService;
    private readonly IDeliveryStateService _deliveryStateService;
    private readonly ITripStateService _tripStateService;
    private readonly IClock _clock;

    public OrderService(
        DroneDeliveryDbContext dbContext,
        IDistanceService distanceService,
        IRoutePlanningService routePlanningService,
        IDeliveryStateService deliveryStateService,
        ITripStateService tripStateService,
        IClock clock)
    {
        _dbContext = dbContext;
        _distanceService = distanceService;
        _routePlanningService = routePlanningService;
        _deliveryStateService = deliveryStateService;
        _tripStateService = tripStateService;
        _clock = clock;
    }

    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var orders = await _dbContext.Orders
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
        var deliveryByOrderId = await _dbContext.Deliveries
            .ToDictionaryAsync(delivery => delivery.OrderId, cancellationToken);

        return orders
            .Select(order =>
            {
                deliveryByOrderId.TryGetValue(order.Id, out var delivery);
                return MapResponse(order, delivery, utcNow);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetQueueAsync(CancellationToken cancellationToken)
    {
        var pendingOrders = await _dbContext.Orders
            .Where(order => order.QueueStatus == OrderQueueStatus.Queued)
            .ToListAsync(cancellationToken);

        return pendingOrders
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.QueuedAtUtc)
            .ThenBy(order => _distanceService.Calculate(0, 0, order.DestinationX, order.DestinationY))
            .Select(order => order.ToResponse())
            .ToList();
    }

    public async Task<OrderResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var order = await FindAsync(id, cancellationToken);
        var delivery = await _dbContext.Deliveries
            .FirstOrDefaultAsync(item => item.OrderId == order.Id, cancellationToken);

        return MapResponse(order, delivery, utcNow);
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        Validate(request.CustomerName, request.PackageWeightKg);
        await ValidateDestinationAsync(request.DestinationX, request.DestinationY, cancellationToken);
        var order = new DeliveryOrder
        {
            CustomerName = request.CustomerName.Trim(),
            DestinationX = request.DestinationX,
            DestinationY = request.DestinationY,
            PackageWeightKg = request.PackageWeightKg,
            Priority = request.Priority,
            Status = OrderStatus.Pending,
            QueueStatus = OrderQueueStatus.NotQueued
        };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order.ToResponse();
    }

    public async Task<OrderResponse> UpdateAsync(int id, UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        Validate(request.CustomerName, request.PackageWeightKg);
        await ValidateDestinationAsync(request.DestinationX, request.DestinationY, cancellationToken);
        var order = await FindAsync(id, cancellationToken);
        order.CustomerName = request.CustomerName.Trim();
        order.DestinationX = request.DestinationX;
        order.DestinationY = request.DestinationY;
        order.PackageWeightKg = request.PackageWeightKg;
        order.Priority = request.Priority;
        order.Status = request.Status;
        order.QueueStatus = request.Status == OrderStatus.Pending ? order.QueueStatus : OrderQueueStatus.Allocated;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order.ToResponse();
    }

    public async Task<OrderResponse> QueueAsync(int id, CancellationToken cancellationToken)
    {
        var order = await FindAsync(id, cancellationToken);

        if (order.QueueStatus == OrderQueueStatus.Queued)
        {
            throw new ValidationException("ORDER_ALREADY_QUEUED", "Order already queued", "The order is already in the planning queue.");
        }

        if (order.Status != OrderStatus.Pending || order.QueueStatus is OrderQueueStatus.Planned or OrderQueueStatus.Allocated or OrderQueueStatus.Completed)
        {
            throw new ValidationException("ORDER_NOT_ELIGIBLE_FOR_QUEUE", "Order not eligible for queue", "Only pending orders outside a trip can be queued.");
        }

        order.QueueStatus = OrderQueueStatus.Queued;
        order.QueuedAtUtc = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order.ToResponse();
    }

    public async Task<OrderResponse> RemoveFromQueueAsync(int id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(item => item.TripOrders)
            .ThenInclude(item => item.Trip)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Order {id} was not found.");

        if (order.TripOrders.Any(item => !_tripStateService.IsMutable(item.Trip, _clock.UtcNow)))
        {
            throw new ValidationException("TRIP_ALREADY_STARTED", "Trip already started", "The order cannot be removed after its trip has started.");
        }

        foreach (var tripOrder in order.TripOrders.Where(item => _tripStateService.IsMutable(item.Trip, _clock.UtcNow)).ToList())
        {
            _dbContext.TripOrders.Remove(tripOrder);
        }

        order.QueueStatus = OrderQueueStatus.NotQueued;
        order.QueuedAtUtc = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order.ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await FindAsync(id, cancellationToken);
        _dbContext.Orders.Remove(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<DeliveryOrder> FindAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders.FirstOrDefaultAsync(order => order.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Order {id} was not found.");
    }

    private static void Validate(string customerName, decimal packageWeightKg)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ValidationException("INVALID_CUSTOMER_NAME", "Invalid customer name", "Customer name is required.");
        }

        if (packageWeightKg <= 0)
        {
            throw new ValidationException("INVALID_PACKAGE_WEIGHT", "Invalid package weight", "Package weight must be greater than zero.");
        }
    }

    private async Task ValidateDestinationAsync(decimal destinationX, decimal destinationY, CancellationToken cancellationToken)
    {
        var isBlocked = await _routePlanningService.IsPointInsideActiveNoFlyZoneAsync(new RoutePoint(destinationX, destinationY), cancellationToken);
        if (isBlocked)
        {
            throw new ValidationException(
                "ORDER_DESTINATION_IN_NO_FLY_ZONE",
                "Order destination is inside a no-fly zone",
                "The order destination is inside an active no-fly zone.");
        }
    }

    private OrderResponse MapResponse(DeliveryOrder order, Delivery? delivery, DateTime utcNow)
    {
        var status = delivery is null
            ? order.Status
            : _deliveryStateService.GetCurrentState(delivery, utcNow).OrderStatus;

        return new OrderResponse(
            order.Id,
            order.CustomerName,
            order.DestinationX,
            order.DestinationY,
            order.PackageWeightKg,
            order.Priority,
            status,
            delivery is null ? order.QueueStatus : GetQueueStatusFromOrderStatus(status),
            order.QueuedAtUtc,
            order.CreatedAt,
            order.UpdatedAt);
    }

    private static OrderQueueStatus GetQueueStatusFromOrderStatus(OrderStatus status) =>
        status == OrderStatus.Delivered ? OrderQueueStatus.Completed : OrderQueueStatus.Allocated;
}
