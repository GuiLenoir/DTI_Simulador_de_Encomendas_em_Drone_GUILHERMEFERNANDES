using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public sealed record TripStateSnapshot(
    TripStatus TripStatus,
    DroneStatus DroneStatus,
    string CurrentPhase,
    DateTime NextPhaseAtUtc,
    int RemainingPhaseSeconds,
    int ProgressPercentage,
    bool IsActive,
    bool IsImmutable);
