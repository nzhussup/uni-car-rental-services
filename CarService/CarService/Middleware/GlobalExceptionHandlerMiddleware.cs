using CarService.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CarService.Middleware;

public class GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            NotAllowedException => (StatusCodes.Status403Forbidden, "Resource Not Allowed"),
            UserIdNotFoundException => (StatusCodes.Status401Unauthorized, "User Not Found"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (exception is NotFoundException notFoundException)
        {
            logger.LogWarning("Resource not found: {Message}", notFoundException.Message);
        }
        else if (exception is NotAllowedException notAllowedException)
        {
            logger.LogWarning("Resource not allowed: {Message}", notAllowedException.Message);
        }
        else if (exception is UserIdNotFoundException userIdNotFoundException)
        {
            logger.LogWarning("User not found: {Message}", userIdNotFoundException.Message);
        }
        else
        {
            logger.LogError(exception, "An unexpected error occurred");
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}