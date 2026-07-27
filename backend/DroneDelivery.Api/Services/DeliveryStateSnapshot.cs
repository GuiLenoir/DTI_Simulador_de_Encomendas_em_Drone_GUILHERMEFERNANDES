using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public sealed record DeliveryStateSnapshot(
    DroneStatus DroneStatus,
    DeliveryStatus DeliveryStatus,
    OrderStatus OrderStatus,
    string CurrentPhase,
    DateTime CurrentPhaseStartedAtUtc,
    DateTime NextPhaseAtUtc,
    DateTime CompletedAtUtc,
    int ElapsedSeconds,
    int RemainingPhaseSeconds,
    int ProgressPercentage,
    bool IsActive);
