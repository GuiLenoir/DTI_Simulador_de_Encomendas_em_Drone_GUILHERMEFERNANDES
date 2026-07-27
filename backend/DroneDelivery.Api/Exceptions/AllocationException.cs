namespace DroneDelivery.Api.Exceptions;

public sealed class AllocationException : ApplicationExceptionBase
{
    public AllocationException(string detail)
        : base("NO_ELIGIBLE_DRONE", "No eligible drone", detail, StatusCodes.Status422UnprocessableEntity)
    {
    }
}
