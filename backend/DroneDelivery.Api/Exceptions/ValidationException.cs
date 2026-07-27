namespace DroneDelivery.Api.Exceptions;

public sealed class ValidationException : ApplicationExceptionBase
{
    public ValidationException(string code, string title, string detail)
        : base(code, title, detail, StatusCodes.Status400BadRequest)
    {
    }
}
