using DroneDelivery.Api.Models;
using DroneDelivery.Api.Options;
using Microsoft.Extensions.Options;

namespace DroneDelivery.Api.Services;

public sealed class ChargingService : IChargingService
{
    private readonly SimulationOptions _simulationOptions;

    public ChargingService(IOptions<SimulationOptions> simulationOptions)
    {
        _simulationOptions = simulationOptions.Value;
    }

    public DroneRuntimeState GetCurrentState(Drone drone, DateTime utcNow)
    {
        if (drone.ChargingStartedAtUtc is null ||
            drone.BatteryAtChargingStartPercentage is null ||
            drone.ChargingCompletedAtUtc is null ||
            utcNow >= drone.ChargingCompletedAtUtc)
        {
            var status = drone.Status == DroneStatus.Charging && drone.ChargingCompletedAtUtc is not null && utcNow >= drone.ChargingCompletedAtUtc
                ? DroneStatus.Idle
                : drone.Status;
            var battery = status == DroneStatus.Idle && drone.ChargingCompletedAtUtc is not null && utcNow >= drone.ChargingCompletedAtUtc
                ? 100m
                : drone.BatteryLevelPercent;

            return new DroneRuntimeState(status, ClampBattery(battery), 100, drone.ChargingStartedAtUtc, drone.ChargingCompletedAtUtc);
        }

        var elapsedSeconds = Math.Max(0, (decimal)(utcNow - drone.ChargingStartedAtUtc.Value).TotalSeconds);
        var currentBattery = ClampBattery(drone.BatteryAtChargingStartPercentage.Value + elapsedSeconds * drone.ChargingRatePercentagePointsPerSecond);
        var totalSeconds = Math.Max(1, (decimal)(drone.ChargingCompletedAtUtc.Value - drone.ChargingStartedAtUtc.Value).TotalSeconds);
        var progress = Math.Clamp((int)Math.Floor(elapsedSeconds / totalSeconds * 100), 0, 99);

        return new DroneRuntimeState(DroneStatus.Charging, currentBattery, progress, drone.ChargingStartedAtUtc, drone.ChargingCompletedAtUtc);
    }

    public void StartChargingIfNeeded(Drone drone, decimal batteryAtReturnPercentage, DateTime completedAtUtc)
    {
        var battery = ClampBattery(batteryAtReturnPercentage);
        drone.BatteryLevelPercent = battery;

        if (battery >= 100m)
        {
            drone.Status = DroneStatus.Idle;
            drone.ChargingStartedAtUtc = null;
            drone.BatteryAtChargingStartPercentage = null;
            drone.ChargingCompletedAtUtc = null;
            return;
        }

        var rate = _simulationOptions.ChargingPercentagePointsPerSecond > 0
            ? _simulationOptions.ChargingPercentagePointsPerSecond
            : drone.ChargingRatePercentagePointsPerSecond;
        var secondsToFull = (int)Math.Ceiling((100m - battery) / rate);

        drone.Status = DroneStatus.Charging;
        drone.ChargingRatePercentagePointsPerSecond = rate;
        drone.ChargingStartedAtUtc = completedAtUtc;
        drone.BatteryAtChargingStartPercentage = battery;
        drone.ChargingCompletedAtUtc = completedAtUtc.AddSeconds(Math.Max(1, secondsToFull));
    }

    private static decimal ClampBattery(decimal value) => Math.Clamp(Math.Round(value, 2), 0m, 100m);
}
