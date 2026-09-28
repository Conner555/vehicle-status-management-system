using VehicleStatusSystem.Exceptions;

namespace VehicleStatusSystem.Exceptions;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
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
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning(
                "Resource not found : {Message}",
                ex.Message
            );

            await WriteErrorAsync(
                context,
                StatusCodes.Status404NotFound,
                ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(
                "Business rule violation : {Message}",
                ex.Message
            );

            await WriteErrorAsync(
                context,
                StatusCodes.Status409Conflict,
                ex.Message);
        }

        catch (InvalidCredentialsException ex)
        {
            _logger.LogWarning(
                "Authentication failed: {Message}",
                ex.Message);

            await WriteErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                ex.Message);
        }

        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred.");

            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An internal server error occurred.");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            new
            {
                statusCode,
                message
            });
    }
}