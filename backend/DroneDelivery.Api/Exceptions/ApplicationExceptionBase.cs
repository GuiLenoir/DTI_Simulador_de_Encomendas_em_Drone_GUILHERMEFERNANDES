namespace DroneDelivery.Api.Exceptions;

public abstract class ApplicationExceptionBase : Exception
{
    protected ApplicationExceptionBase(string code, string title, string detail, int statusCode)
        : base(detail)
    {
        Code = code;
        Title = title;
        Detail = detail;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public string Title { get; }
    public string Detail { get; }
    public int StatusCode { get; }
}
