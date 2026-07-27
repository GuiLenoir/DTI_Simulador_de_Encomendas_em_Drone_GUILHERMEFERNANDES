using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneDelivery.Api.Services;

public sealed class NoFlyZoneService : INoFlyZoneService
{
    private readonly DroneDeliveryDbContext _dbContext;

    public NoFlyZoneService(DroneDeliveryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<NoFlyZoneResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var zones = await _dbContext.NoFlyZones
            .Include(zone => zone.Points)
            .OrderBy(zone => zone.Name)
            .ThenBy(zone => zone.Id)
            .ToListAsync(cancellationToken);

        return zones.Select(MapResponse).ToList();
    }

    public async Task<NoFlyZoneResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var zone = await FindAsync(id, cancellationToken);
        return MapResponse(zone);
    }

    public async Task<NoFlyZoneResponse> CreateAsync(CreateNoFlyZoneRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request.Name, request.Points);
        var zone = new NoFlyZone
        {
            Name = request.Name.Trim(),
            IsActive = request.IsActive
        };

        AddPoints(zone, request.Points);
        _dbContext.NoFlyZones.Add(zone);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapResponse(zone);
    }

    public async Task<NoFlyZoneResponse> UpdateAsync(int id, UpdateNoFlyZoneRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request.Name, request.Points);
        var zone = await FindAsync(id, cancellationToken);
        zone.Name = request.Name.Trim();
        zone.IsActive = request.IsActive;
        zone.Points.Clear();
        AddPoints(zone, request.Points);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapResponse(zone);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var zone = await FindAsync(id, cancellationToken);
        _dbContext.NoFlyZones.Remove(zone);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<NoFlyZone> FindAsync(int id, CancellationToken cancellationToken) =>
        await _dbContext.NoFlyZones
            .Include(zone => zone.Points)
            .FirstOrDefaultAsync(zone => zone.Id == id, cancellationToken)
        ?? throw new NotFoundException($"No-fly zone {id} was not found.");

    private static void ValidateRequest(string name, IReadOnlyList<NoFlyZonePointRequest> points)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("NO_FLY_ZONE_NAME_REQUIRED", "No-fly zone name is required", "The no-fly zone name is required.");
        }

        if (points.Count < 3)
        {
            throw new ValidationException("NO_FLY_ZONE_REQUIRES_POLYGON", "Invalid no-fly zone polygon", "A no-fly zone requires at least three points.");
        }
    }

    private static void AddPoints(NoFlyZone zone, IReadOnlyList<NoFlyZonePointRequest> points)
    {
        for (var index = 0; index < points.Count; index++)
        {
            zone.Points.Add(new NoFlyZonePoint
            {
                Sequence = index + 1,
                X = points[index].X,
                Y = points[index].Y
            });
        }
    }

    private static NoFlyZoneResponse MapResponse(NoFlyZone zone) =>
        new(
            zone.Id,
            zone.Name,
            zone.IsActive,
            zone.Points
                .OrderBy(point => point.Sequence)
                .Select(point => new NoFlyZonePointResponse(point.X, point.Y))
                .ToList(),
            zone.CreatedAtUtc,
            zone.UpdatedAtUtc);
}
