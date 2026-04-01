using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DartsTournament.Api.Middleware;

public class ExceptionHandlingMiddleware
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case InvalidOperationException ex:
                // Business logic errors
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = ex.Message;
                errorResponse.Code = "BUSINESS_ERROR";
                _logger.LogWarning(ex, "Business logic error: {Message}", ex.Message);
                break;

            case UnauthorizedAccessException ex:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = "Accès non autorisé";
                errorResponse.Code = "UNAUTHORIZED";
                _logger.LogWarning(ex, "Unauthorized access attempt");
                break;

            case KeyNotFoundException ex:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = ex.Message;
                errorResponse.Code = "NOT_FOUND";
                _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
                break;

            case DbUpdateConcurrencyException ex:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Message = "Les données ont été modifiées par un autre utilisateur. Veuillez rafraîchir.";
                errorResponse.Code = "CONCURRENCY_ERROR";
                _logger.LogWarning(ex, "Concurrency conflict");
                break;

            case DbUpdateException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Erreur lors de la sauvegarde des données";
                errorResponse.Code = "DATABASE_ERROR";
                _logger.LogError(ex, "Database update error: {Message}", ex.InnerException?.Message ?? ex.Message);
                break;

            case ArgumentException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = ex.Message;
                errorResponse.Code = "INVALID_ARGUMENT";
                _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "Une erreur inattendue s'est produite";
                errorResponse.Code = "INTERNAL_ERROR";
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                break;
        }

        var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(result);
    }
}

public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
