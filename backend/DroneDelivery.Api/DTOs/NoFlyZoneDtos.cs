namespace DroneDelivery.Api.DTOs;

public sealed record NoFlyZonePointRequest(decimal X, decimal Y);

public sealed record CreateNoFlyZoneRequest(
    string Name,
    bool IsActive,
    IReadOnlyList<NoFlyZonePointRequest> Points);

public sealed record UpdateNoFlyZoneRequest(
    string Name,
    bool IsActive,
    IReadOnlyList<NoFlyZonePointRequest> Points);

public sealed record NoFlyZonePointResponse(decimal X, decimal Y);

public sealed record NoFlyZoneResponse(
    int Id,
    string Name,
    bool IsActive,
    IReadOnlyList<NoFlyZonePointResponse> Points,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
