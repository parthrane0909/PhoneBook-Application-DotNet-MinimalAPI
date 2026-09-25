using System.Text.Json;
using Phonebook.Middleware;

namespace Phonebook.Validation;

/// <summary>
/// Reads a JSON request body the way the old [ApiController] pipeline did:
/// an unsupported content type → 415 problem+json, an unparsable body →
/// 400 {"detail": ...}, then DataAnnotations validation of the deserialized DTO.
/// </summary>
public static class JsonBody
{
    // Every body parameter of the old controllers was named "request", so MVC's
    // implicit "required" error for a failed body bind always used that name.
    private const string ParameterName = "request";

    public static async Task<T> ReadAsync<T>(HttpRequest request, JsonSerializerOptions options)
        where T : class
    {
        if (!HasJsonContentType(request.ContentType))
        {
            throw new UnsupportedMediaTypeException();
        }

        T? body;

        try
        {
            body = await JsonSerializer.DeserializeAsync<T>(
                request.Body, options, request.HttpContext.RequestAborted);
        }
        catch (JsonException ex)
        {
            // Same two model-state errors MVC recorded for a broken JSON payload:
            // the failed parameter plus the System.Text.Json error itself.
            // ErrorResponses turns that pair into either the joined detail
            // ("request: ...; $.name: ...") or "Invalid request body".
            var errors = new List<RequestError>
            {
                new(ParameterName, "The request field is required."),
                new(ex.Path ?? string.Empty, ex.Message),
            };

            throw new BadRequestException(ErrorResponses.Build(errors));
        }

        if (body is null)
        {
            // The payload was the JSON literal `null` (Java: empty body).
            throw new BadRequestException("Invalid request body");
        }

        RequestValidation.Validate(body);
        return body;
    }

    // Mirrors the media types the MVC JSON input formatter accepted:
    // application/json, text/json, and any "+json" structured suffix.
    private static bool HasJsonContentType(string? contentType)
    {
        if (contentType is null)
        {
            return false;
        }

        var mediaType = contentType.Split(';')[0].Trim();

        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("text/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }
}
