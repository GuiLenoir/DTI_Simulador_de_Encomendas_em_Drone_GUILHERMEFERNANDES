using DroneDelivery.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DroneDelivery.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApplicationExceptionBase exception)
        {
            var problem = new ProblemDetails
            {
                Title = exception.Title,
                Detail = exception.Detail,
                Status = exception.StatusCode,
                Type = exception.Code
            };
            context.Response.StatusCode = exception.StatusCode;
            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected API error.");
            var problem = new ProblemDetails
            {
                Title = "Unexpected error",
                Detail = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                Type = "UNEXPECTED_ERROR"
            };
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
