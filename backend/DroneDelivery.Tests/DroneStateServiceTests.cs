using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using DroneDelivery.Api.Services;

namespace DroneDelivery.Tests;

public sealed class DroneStateServiceTests
{
    [Fact]
    public void Transition_RejectsInvalidTransition()
    {
        var drone = new Drone { Status = DroneStatus.Idle };
        var service = new DroneStateService();

        Assert.Throws<ValidationException>(() => service.Transition(drone, DroneStatus.Flying));
    }

    [Fact]
    public void Transition_AllowsDeliveryFlow()
    {
        var drone = new Drone { Status = DroneStatus.Idle };
        var service = new DroneStateService();

        service.Transition(drone, DroneStatus.Loading);
        service.Transition(drone, DroneStatus.Flying);
        service.Transition(drone, DroneStatus.Delivering);
        service.Transition(drone, DroneStatus.Returning);
        service.Transition(drone, DroneStatus.Idle);

        Assert.Equal(DroneStatus.Idle, drone.Status);
    }
}
