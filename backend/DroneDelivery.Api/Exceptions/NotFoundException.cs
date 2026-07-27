namespace DroneDelivery.Api.Exceptions;

public sealed class NotFoundException : ApplicationExceptionBase
{
    public NotFoundException(string detail)
        : base("NOT_FOUND", "Resource not found", detail, StatusCodes.Status404NotFound)
    {
    }
}
