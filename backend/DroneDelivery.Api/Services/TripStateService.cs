using DroneDelivery.Api.Models;

namespace DroneDelivery.Api.Services;

public sealed class TripStateService : ITripStateService
{
    public TripStateSnapshot GetCurrentState(Trip trip, DateTime utcNow)
    {
        if (utcNow < trip.LoadingStartedAtUtc)
        {
            return CreateSnapshot(trip, utcNow, TripStatus.Planned, DroneStatus.Idle, "Planned", trip.LoadingStartedAtUtc, isActive: false, isImmutable: false);
        }

        if (utcNow < trip.FlyingStartedAtUtc)
        {
            return CreateSnapshot(trip, utcNow, TripStatus.Loading, DroneStatus.Loading, "Loading", trip.FlyingStartedAtUtc, isActive: true, isImmutable: true);
        }

        if (utcNow < trip.DeliveringStartedAtUtc)
        {
            return CreateSnapshot(trip, utcNow, TripStatus.Flying, DroneStatus.Flying, "Flying", trip.DeliveringStartedAtUtc, isActive: true, isImmutable: true);
        }

        if (utcNow < trip.ReturningStartedAtUtc)
        {
            return CreateSnapshot(trip, utcNow, TripStatus.Delivering, DroneStatus.Delivering, "Delivering", trip.ReturningStartedAtUtc, isActive: true, isImmutable: true);
        }

        if (utcNow < trip.CompletedAtUtc)
        {
            return CreateSnapshot(trip, utcNow, TripStatus.Returning, DroneStatus.Returning, "Returning", trip.CompletedAtUtc, isActive: true, isImmutable: true);
        }

        return CreateSnapshot(trip, utcNow, TripStatus.Completed, DroneStatus.Charging, "Completed", trip.CompletedAtUtc, isActive: false, isImmutable: true);
    }

    public bool IsMutable(Trip trip, DateTime utcNow) => utcNow < trip.LoadingStartedAtUtc && trip.Status == TripStatus.Planned;

    private static TripStateSnapshot CreateSnapshot(
        Trip trip,
        DateTime utcNow,
        TripStatus tripStatus,
        DroneStatus droneStatus,
        string currentPhase,
        DateTime nextPhaseAtUtc,
        bool isActive,
        bool isImmutable)
    {
        var totalSeconds = Math.Max(1, (int)Math.Ceiling((trip.CompletedAtUtc - trip.PlannedAtUtc).TotalSeconds));
        var elapsedSeconds = Math.Clamp((int)Math.Floor((utcNow - trip.PlannedAtUtc).TotalSeconds), 0, totalSeconds);
        var remainingPhaseSeconds = Math.Max(0, (int)Math.Ceiling((nextPhaseAtUtc - utcNow).TotalSeconds));
        var progressPercentage = Math.Clamp((int)Math.Floor(elapsedSeconds / (double)totalSeconds * 100), 0, 100);

        return new TripStateSnapshot(tripStatus, droneStatus, currentPhase, nextPhaseAtUtc, remainingPhaseSeconds, progressPercentage, isActive, isImmutable);
    }
}
