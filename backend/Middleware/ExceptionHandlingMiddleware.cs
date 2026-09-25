using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Phonebook.Middleware;

/// <summary>
/// Turns exceptions into the {"detail": "..."} error responses the API contract
/// requires. This replaces the Java GlobalExceptionHandler (@RestControllerAdvice).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (NotFoundException ex)
        {
            await WriteError(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (BadRequestException ex)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (DbUpdateException)
        {
            // Same as Java: every database integrity violation (unique phone/email, ...)
            // comes back as HTTP 400 with this message.
            await WriteError(
                context,
                StatusCodes.Status400BadRequest,
                "Phone number or email already exists");
        }
        catch (BadHttpRequestException)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "Invalid request body");
        }
        catch (UnsupportedMediaTypeException)
        {
            await WriteUnsupportedMediaType(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled API error");
            await WriteError(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal server error");
        }
    }

    private static async Task WriteError(HttpContext context, int statusCode, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            new Dictionary<string, string> { ["detail"] = detail });
    }

    private static async Task WriteUnsupportedMediaType(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
        context.Response.ContentType = "application/problem+json; charset=utf-8";

        // Same problem-details body ASP.NET Core MVC returned for an unsupported
        // request content type (traceId = current W3C trace id). Serialized with the
        // default options on purpose: this body keeps camelCase field names even
        // though responses are snake_case everywhere else.
        var payload = JsonSerializer.Serialize(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.16",
            title = "Unsupported Media Type",
            status = StatusCodes.Status415UnsupportedMediaType,
            traceId = Activity.Current?.Id ?? context.TraceIdentifier,
        });

        await context.Response.WriteAsync(payload);
    }
}
